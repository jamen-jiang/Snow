using System.Data;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Excel;

/// <summary>
/// 静默 Excel 实现: 不读写任何文件, 一被调用就报错并留日志
/// <para>作为 <see cref="IExcelService"/> 的兜底实现, 引入 Snow.Excel.ClosedXML 后由
/// <c>ClosedXmlExcelService</c> 顶替(它带 ReplaceServices)。</para>
/// <para>与 <c>NullFtpClient</c> 的取舍一致: 这里<b>不假装成功</b>。导出返回空字节会让调用方写出一个坏文件,
/// 导入返回空列表会被当成"表里没数据"——都在更远的地方才炸。当场报错最便宜。</para>
/// </summary>
[TransientService]
public sealed class NullExcelService : IExcelService
{
    private readonly ILogger<NullExcelService>? _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="logger">日志</param>
    public NullExcelService(ILogger<NullExcelService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public byte[] Export<T>(IEnumerable<T> rows, Action<ExcelExportOptions>? configure = null)
    {
        throw NotIntroduced("导出");
    }

    /// <inheritdoc />
    public byte[] Export(DataTable table, Action<ExcelExportOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(table);

        throw NotIntroduced("导出(DataTable)");
    }

    /// <inheritdoc />
    public Task<byte[]> ExportAsync<T>(IEnumerable<T> rows, Action<ExcelExportOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        throw NotIntroduced("导出");
    }

    /// <inheritdoc />
    public List<T> Import<T>(Stream stream, Action<ExcelImportOptions>? configure = null) where T : new()
    {
        ArgumentNullException.ThrowIfNull(stream);

        throw NotIntroduced("导入");
    }

    /// <inheritdoc />
    public List<T> Import<T>(byte[] data, Action<ExcelImportOptions>? configure = null) where T : new()
    {
        ArgumentNullException.ThrowIfNull(data);

        throw NotIntroduced("导入");
    }

    /// <inheritdoc />
    public List<T> Import<T>(string filePath, Action<ExcelImportOptions>? configure = null) where T : new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        throw NotIntroduced("导入");
    }

    /// <summary>
    /// 记一条错误日志并给出引导性异常
    /// </summary>
    /// <param name="operation">操作名</param>
    /// <returns>待抛出的异常</returns>
    private InvalidOperationException NotIntroduced(string operation)
    {
        const string message = "Excel 操作未执行: 当前是静默实现, 请引入 Snow.Excel.ClosedXML 包。";

        _logger?.LogError("Excel {Operation} 未执行(当前为静默实现, 需引入 Snow.Excel.ClosedXML)", operation);

        return new InvalidOperationException(message);
    }
}
