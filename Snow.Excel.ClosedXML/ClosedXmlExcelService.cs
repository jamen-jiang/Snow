using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using Snow.DependencyInjection;

namespace Snow.Excel.ClosedXML;

/// <summary>
/// 基于 ClosedXML 的 Excel 服务: 按属性映射写 xlsx, 按表头文字映射读回强类型列表
/// <para>顶替契约包的 <see cref="NullExcelService"/>: 引入本包后注入到的 <see cref="IExcelService"/> 就是它。</para>
/// <para>无状态(列映射按类型缓存), 可安全并发调用。</para>
/// </summary>
[TransientService(ReplaceServices = true)]
public sealed class ClosedXmlExcelService : IExcelService, IClosedXmlExcelService
{
    /// <inheritdoc />
    public XLWorkbook CreateWorkbook()
    {
        return new XLWorkbook();
    }

    /// <inheritdoc />
    public XLWorkbook OpenWorkbook(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new XLWorkbook(stream);
    }

    /// <inheritdoc />
    /// <remarks>ClosedXML 读完前不能释放流, 用只读副本包一层, 返回后副本即可回收</remarks>
    public XLWorkbook OpenWorkbook(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using MemoryStream stream = new(data, writable: false);

        return new XLWorkbook(stream);
    }

    /// <inheritdoc />
    public byte[] Export<T>(IEnumerable<T> rows, Action<ExcelExportOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(rows);

        ExcelExportOptions options = BuildExportOptions(configure);
        IReadOnlyList<ExcelColumn> columns = ExcelColumnMap.For(typeof(T));

        using XLWorkbook workbook = CreateWorkbook();
        IXLWorksheet sheet = workbook.AddWorksheet(options.SheetName);
        int lastRow = FillSheet(sheet, rows, columns, options);
        ApplySheetOptions(sheet, options, lastRow, columns.Count);

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <inheritdoc />
    public byte[] Export(DataTable table, Action<ExcelExportOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(table);

        ExcelExportOptions options = BuildExportOptions(configure);

        using XLWorkbook workbook = CreateWorkbook();
        IXLWorksheet sheet = workbook.AddWorksheet(options.SheetName);

        int rowIndex = 1;
        if (options.IncludeHeaderRow)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                DataColumn column = table.Columns[i];
                IXLCell cell = sheet.Cell(rowIndex, i + 1);
                cell.Value = string.IsNullOrWhiteSpace(column.Caption) ? column.ColumnName : column.Caption;
                cell.Style.Font.Bold = true;
            }

            rowIndex++;
        }

        foreach (DataRow row in table.Rows)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                WriteCell(sheet.Cell(rowIndex, i + 1), row[i] is DBNull ? null : row[i], options.DateTimeFormat, null);
            }

            rowIndex++;
        }

        ApplySheetOptions(sheet, options, Math.Max(rowIndex - 1, 1), table.Columns.Count);

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <inheritdoc />
    public Task<byte[]> ExportAsync<T>(IEnumerable<T> rows, Action<ExcelExportOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Export(rows, configure));
    }

    /// <inheritdoc />
    public List<T> Import<T>(Stream stream, Action<ExcelImportOptions>? configure = null) where T : new()
    {
        ArgumentNullException.ThrowIfNull(stream);

        using XLWorkbook workbook = OpenWorkbook(stream);

        return Read<T>(workbook, BuildImportOptions(configure));
    }

    /// <inheritdoc />
    public List<T> Import<T>(byte[] data, Action<ExcelImportOptions>? configure = null) where T : new()
    {
        ArgumentNullException.ThrowIfNull(data);

        using XLWorkbook workbook = OpenWorkbook(data);

        return Read<T>(workbook, BuildImportOptions(configure));
    }

    /// <inheritdoc />
    public List<T> Import<T>(string filePath, Action<ExcelImportOptions>? configure = null) where T : new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("要导入的 Excel 文件不存在: " + filePath, filePath);
        }

        using FileStream stream = File.OpenRead(filePath);

        return Import<T>(stream, configure);
    }

    /// <summary>
    /// 写表头与数据行, 返回最后一行的行号
    /// </summary>
    /// <typeparam name="T">行类型</typeparam>
    /// <param name="sheet">工作表</param>
    /// <param name="rows">数据行</param>
    /// <param name="columns">列映射</param>
    /// <param name="options">导出选项</param>
    /// <returns>最后一行行号(空表且不写表头时为 0, 这里至少返回 1 便于后续选区)</returns>
    private static int FillSheet<T>(IXLWorksheet sheet, IEnumerable<T> rows, IReadOnlyList<ExcelColumn> columns, ExcelExportOptions options)
    {
        int rowIndex = 1;

        if (options.IncludeHeaderRow)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                IXLCell cell = sheet.Cell(rowIndex, i + 1);
                cell.Value = columns[i].Header;
                cell.Style.Font.Bold = true;
            }

            rowIndex++;
        }

        foreach (T item in rows)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                ExcelColumn column = columns[i];
                WriteCell(sheet.Cell(rowIndex, i + 1), column.Property.GetValue(item), options.DateTimeFormat, column.Format);
            }

            rowIndex++;
        }

        return Math.Max(rowIndex - 1, 1);
    }

    /// <summary>
    /// 冻结表头、加自动筛选、按内容调列宽
    /// </summary>
    /// <param name="sheet">工作表</param>
    /// <param name="options">导出选项</param>
    /// <param name="lastRow">最后一行行号</param>
    /// <param name="columnCount">列数</param>
    private static void ApplySheetOptions(IXLWorksheet sheet, ExcelExportOptions options, int lastRow, int columnCount)
    {
        if (columnCount <= 0)
        {
            return;
        }

        if (options.FreezeHeaderRow && options.IncludeHeaderRow)
        {
            sheet.SheetView.FreezeRows(1);
        }

        if (options.AutoFilter && options.IncludeHeaderRow)
        {
            sheet.Range(1, 1, lastRow, columnCount).SetAutoFilter();
        }

        if (options.AutoFitColumns)
        {
            sheet.Columns(1, columnCount).AdjustToContents();
        }
    }

    /// <summary>
    /// 按值的实际类型写单元格: 日期带格式, 数字保持数值类型, 其余转字符串
    /// </summary>
    /// <param name="cell">目标单元格</param>
    /// <param name="value">值, 可空</param>
    /// <param name="dateTimeFormat">日期时间的默认格式</param>
    /// <param name="columnFormat">列上指定的格式(<see cref="ExcelColumnAttribute.Format"/>), 可空; 数字列用它, 日期列优先用它</param>
    private static void WriteCell(IXLCell cell, object? value, string dateTimeFormat, string? columnFormat)
    {
        // 日期列: 列上指定了格式就用列的, 否则用全局日期格式; 数字列: 只有列上指定时才设格式
        string dateFormat = string.IsNullOrWhiteSpace(columnFormat) ? dateTimeFormat : columnFormat;

        switch (value)
        {
            case null:
                break;
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.DateFormat.Format = dateFormat;
                break;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.DateTime;
                cell.Style.DateFormat.Format = dateFormat;
                break;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                cell.Style.DateFormat.Format = dateFormat;
                break;
            case TimeOnly time:
                cell.Value = time.ToTimeSpan();
                break;
            case TimeSpan timeSpan:
                cell.Value = timeSpan;
                break;
            case bool boolean:
                cell.Value = boolean;
                break;
            case Guid guid:
                cell.Value = guid.ToString();
                break;
            case Enum enumeration:
                cell.Value = enumeration.ToString();
                break;
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                cell.Value = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(columnFormat))
                {
                    cell.Style.NumberFormat.Format = columnFormat;
                }

                break;
            default:
                cell.Value = value.ToString() ?? string.Empty;
                break;
        }
    }

    /// <summary>
    /// 读工作簿到强类型列表
    /// </summary>
    /// <typeparam name="T">行类型</typeparam>
    /// <param name="workbook">工作簿</param>
    /// <param name="options">导入选项</param>
    /// <returns>数据行</returns>
    private static List<T> Read<T>(XLWorkbook workbook, ExcelImportOptions options) where T : new()
    {
        IXLWorksheet sheet = ResolveWorksheet(workbook, options);
        IReadOnlyList<ExcelColumn> columns = ExcelColumnMap.For(typeof(T));

        int columnCount = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (columnCount == 0 || columns.Count == 0)
        {
            return [];
        }

        List<(int Column, ExcelColumn Map)> bindings = ResolveBindings(sheet, columns, options, columnCount);
        if (bindings.Count == 0)
        {
            throw new InvalidOperationException(
                "表头与 " + typeof(T).Name + " 的属性名没有一处匹配: 实际表头为 " + DescribeHeaders(sheet, options, columnCount) + "。");
        }

        int firstRow = options.HasHeaderRow ? options.HeaderRowNumber + 1 : 1;
        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

        List<T> result = [];
        for (int rowNumber = firstRow; rowNumber <= lastRow; rowNumber++)
        {
            T item = new();
            bool anyValue = false;

            foreach ((int column, ExcelColumn map) in bindings)
            {
                IXLCell cell = sheet.Cell(rowNumber, column);
                if (cell.IsEmpty())
                {
                    continue;
                }

                object? value = ReadCell(cell, map.Property.PropertyType, options.TrimValues);
                if (value is null)
                {
                    continue;
                }

                map.Property.SetValue(item, value);
                anyValue = true;
            }

            if (anyValue || !options.SkipEmptyRows)
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>
    /// 定工作表: 有名字按名字, 否则按序号
    /// </summary>
    /// <param name="workbook">工作簿</param>
    /// <param name="options">导入选项</param>
    /// <returns>工作表</returns>
    private static IXLWorksheet ResolveWorksheet(XLWorkbook workbook, ExcelImportOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SheetName))
        {
            return workbook.Worksheets.FirstOrDefault(sheet => string.Equals(sheet.Name, options.SheetName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("工作簿里没有名为 " + options.SheetName + " 的工作表, 现有: " + string.Join(", ", workbook.Worksheets.Select(sheet => sheet.Name)) + "。");
        }

        int index = options.SheetIndex + 1;
        if (workbook.Worksheets.Count == 0 || index < 1 || index > workbook.Worksheets.Count)
        {
            throw new InvalidOperationException("工作簿里没有第 " + (options.SheetIndex + 1) + " 个工作表(共 " + workbook.Worksheets.Count + " 个)。");
        }

        return workbook.Worksheet(index);
    }

    /// <summary>
    /// 算列绑定: 有表头按文字匹配(忽略大小写), 无表头按属性顺序对列
    /// </summary>
    /// <param name="sheet">工作表</param>
    /// <param name="columns">列映射</param>
    /// <param name="options">导入选项</param>
    /// <param name="columnCount">实际列数</param>
    /// <returns>列号与属性映射的对应关系</returns>
    private static List<(int Column, ExcelColumn Map)> ResolveBindings(IXLWorksheet sheet, IReadOnlyList<ExcelColumn> columns, ExcelImportOptions options, int columnCount)
    {
        List<(int Column, ExcelColumn Map)> bindings = [];

        if (!options.HasHeaderRow)
        {
            for (int i = 0; i < columns.Count && i < columnCount; i++)
            {
                bindings.Add((i + 1, columns[i]));
            }

            return bindings;
        }

        Dictionary<string, int> headers = new(StringComparer.OrdinalIgnoreCase);
        for (int column = 1; column <= columnCount; column++)
        {
            string text = sheet.Cell(options.HeaderRowNumber, column).GetString().Trim();
            if (text.Length > 0 && !headers.ContainsKey(text))
            {
                headers[text] = column;
            }
        }

        foreach (ExcelColumn map in columns)
        {
            if (headers.TryGetValue(map.Header, out int column))
            {
                bindings.Add((column, map));
            }
        }

        return bindings;
    }

    /// <summary>
    /// 拼出实际表头, 用于报错提示
    /// </summary>
    /// <param name="sheet">工作表</param>
    /// <param name="options">导入选项</param>
    /// <param name="columnCount">实际列数</param>
    /// <returns>逗号分隔的表头文字</returns>
    private static string DescribeHeaders(IXLWorksheet sheet, ExcelImportOptions options, int columnCount)
    {
        List<string> headers = [];
        for (int column = 1; column <= columnCount; column++)
        {
            headers.Add(sheet.Cell(options.HeaderRowNumber, column).GetString().Trim());
        }

        return "[" + string.Join(", ", headers) + "]";
    }

    /// <summary>
    /// 按目标属性类型读单元格: 空单元格返回 null, 枚举/Guid/日期/数字/字符串分别处理
    /// </summary>
    /// <param name="cell">来源单元格</param>
    /// <param name="declaredType">属性声明类型(可能可空)</param>
    /// <param name="trim">字符串是否去首尾空白</param>
    /// <returns>转换后的值</returns>
    private static object? ReadCell(IXLCell cell, Type declaredType, bool trim)
    {
        Type targetType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (cell.IsEmpty())
        {
            return null;
        }

        if (targetType == typeof(string))
        {
            string text = cell.GetFormattedString();
            return trim ? text.Trim() : text;
        }

        if (targetType.IsEnum)
        {
            string text = cell.GetString().Trim();
            return text.Length == 0 ? null : Enum.Parse(targetType, text, ignoreCase: true);
        }

        if (targetType == typeof(Guid))
        {
            string text = cell.GetString().Trim();
            return text.Length == 0 ? null : Guid.Parse(text);
        }

        XLCellValue value = cell.Value;

        if (targetType == typeof(bool))
        {
            return value.IsBoolean ? value.GetBoolean() : bool.Parse(cell.GetString().Trim());
        }

        if (targetType == typeof(DateTime))
        {
            return value.IsDateTime ? value.GetDateTime() : DateTime.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(DateOnly))
        {
            DateTime dateTime = value.IsDateTime ? value.GetDateTime() : DateTime.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
            return DateOnly.FromDateTime(dateTime);
        }

        if (targetType == typeof(TimeOnly))
        {
            return value.IsTimeSpan ? TimeOnly.FromTimeSpan(value.GetTimeSpan()) : TimeOnly.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(TimeSpan))
        {
            return value.IsTimeSpan ? value.GetTimeSpan() : TimeSpan.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(DateTimeOffset))
        {
            return value.IsDateTime ? new DateTimeOffset(value.GetDateTime()) : DateTimeOffset.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
        }

        if (value.IsNumber)
        {
            double number = value.GetNumber();

            // Excel 的数值本质是 double: 9.9 直接转 decimal 会带出 9.900000000000000355... 这种二进制尾巴,
            // 先取 double 的最短往返字符串再解析, 得到的就是人写进去的那个小数
            if (targetType == typeof(decimal))
            {
                return decimal.Parse(number.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(float))
            {
                return (float)number;
            }

            if (targetType == typeof(double))
            {
                return number;
            }

            return Convert.ChangeType(number, targetType, CultureInfo.InvariantCulture);
        }

        string raw = cell.GetString();
        if (trim)
        {
            raw = raw.Trim();
        }

        if (raw.Length == 0)
        {
            return null;
        }

        TypeConverter converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            return converter.ConvertFromInvariantString(raw);
        }

        return Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 复制选项并补默认值
    /// </summary>
    /// <param name="configure">调用方的配置委托</param>
    /// <returns>生效选项</returns>
    private static ExcelExportOptions BuildExportOptions(Action<ExcelExportOptions>? configure)
    {
        ExcelExportOptions options = new();
        configure?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.SheetName))
        {
            options.SheetName = "Sheet1";
        }

        if (string.IsNullOrWhiteSpace(options.DateTimeFormat))
        {
            options.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        }

        return options;
    }

    /// <summary>
    /// 复制选项并补默认值
    /// </summary>
    /// <param name="configure">调用方的配置委托</param>
    /// <returns>生效选项</returns>
    private static ExcelImportOptions BuildImportOptions(Action<ExcelImportOptions>? configure)
    {
        ExcelImportOptions options = new();
        configure?.Invoke(options);

        if (options.HeaderRowNumber < 1)
        {
            options.HeaderRowNumber = 1;
        }

        return options;
    }
}
