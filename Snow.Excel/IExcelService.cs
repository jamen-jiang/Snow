using System.Data;

namespace Snow.Excel;

/// <summary>
/// Excel 导出与导入
/// <para>按属性映射: 列名默认取属性名, 用 <see cref="ExcelColumnAttribute"/> 改名/调序/设格式, 用
/// <see cref="ExcelIgnoreAttribute"/> 排除。导出支持任意对象集合与 <see cref="DataTable"/>;
/// 导入按表头文字匹配属性, 返回强类型列表。</para>
/// <para>默认实现是 <see cref="NullExcelService"/>(调用即报错), 引入 Snow.Excel.ClosedXML 后由
/// <c>ClosedXmlExcelService</c> 顶替。</para>
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// 把对象集合导成 xlsx 字节
    /// </summary>
    /// <typeparam name="T">行类型</typeparam>
    /// <param name="rows">数据行(延迟枚举, 只遍历一次)</param>
    /// <param name="configure">导出选项, 可空</param>
    /// <returns>xlsx 文件内容</returns>
    byte[] Export<T>(IEnumerable<T> rows, Action<ExcelExportOptions>? configure = null);

    /// <summary>
    /// 把 <see cref="DataTable"/> 导成 xlsx 字节(列名取 Caption, 为空时取 ColumnName)
    /// </summary>
    /// <param name="table">数据表</param>
    /// <param name="configure">导出选项, 可空</param>
    /// <returns>xlsx 文件内容</returns>
    byte[] Export(DataTable table, Action<ExcelExportOptions>? configure = null);

    /// <summary>
    /// 异步版导出(实现可能仍是同步计算, 只提供异步签名便于调用方统一写法)
    /// </summary>
    /// <typeparam name="T">行类型</typeparam>
    /// <param name="rows">数据行</param>
    /// <param name="configure">导出选项, 可空</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>xlsx 文件内容</returns>
    Task<byte[]> ExportAsync<T>(IEnumerable<T> rows, Action<ExcelExportOptions>? configure = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从流导入
    /// </summary>
    /// <typeparam name="T">行类型, 需有公共无参构造器</typeparam>
    /// <param name="stream">xlsx 内容流(不会被释放)</param>
    /// <param name="configure">导入选项, 可空</param>
    /// <returns>解析出的行</returns>
    List<T> Import<T>(Stream stream, Action<ExcelImportOptions>? configure = null) where T : new();

    /// <summary>
    /// 从字节导入
    /// </summary>
    /// <typeparam name="T">行类型, 需有公共无参构造器</typeparam>
    /// <param name="data">xlsx 文件内容</param>
    /// <param name="configure">导入选项, 可空</param>
    /// <returns>解析出的行</returns>
    List<T> Import<T>(byte[] data, Action<ExcelImportOptions>? configure = null) where T : new();

    /// <summary>
    /// 从文件导入
    /// </summary>
    /// <typeparam name="T">行类型, 需有公共无参构造器</typeparam>
    /// <param name="filePath">xlsx 文件路径</param>
    /// <param name="configure">导入选项, 可空</param>
    /// <returns>解析出的行</returns>
    List<T> Import<T>(string filePath, Action<ExcelImportOptions>? configure = null) where T : new();
}
