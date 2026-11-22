namespace Snow.Excel;

/// <summary>
/// 导入选项
/// </summary>
public class ExcelImportOptions
{
    /// <summary>
    /// 工作表名; 留空则按 <see cref="SheetIndex"/> 取
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// 工作表序号(从 0 开始), 默认第一个; <see cref="SheetName"/> 有值时忽略
    /// </summary>
    public int SheetIndex { get; set; }

    /// <summary>
    /// 是否有表头行, 默认 true
    /// <para>有: 第 <see cref="HeaderRowNumber"/> 行作表头, 按列名匹配属性, 数据从下一行开始;
    /// 无: 按属性顺序对列, 数据从第一行开始。</para>
    /// </summary>
    public bool HasHeaderRow { get; set; } = true;

    /// <summary>
    /// 表头所在行号(从 1 开始), 默认 1
    /// </summary>
    public int HeaderRowNumber { get; set; } = 1;

    /// <summary>
    /// 字符串值是否去掉首尾空白, 默认 true
    /// </summary>
    public bool TrimValues { get; set; } = true;

    /// <summary>
    /// 是否跳过所有列都为空的整行, 默认 true
    /// </summary>
    public bool SkipEmptyRows { get; set; } = true;
}
