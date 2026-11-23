using Snow.Modularity;

namespace Snow.Excel;

/// <summary>
/// Excel 模块: 本包只有契约(<see cref="IExcelService"/> / 列映射注解 / 选项)与兜底实现
/// <see cref="NullExcelService"/>
/// <para>不引入实现包也能启动, 但一旦调用导出或导入就抛 <see cref="InvalidOperationException"/> 提示安装
/// Snow.Excel.ClosedXML(它带 ReplaceServices 顶替 <see cref="NullExcelService"/>)。</para>
/// <para>本包没有自己的配置节: Excel 的格式选择是逐次调用给的(<see cref="ExcelExportOptions"/> /
/// <see cref="ExcelImportOptions"/>), 不像 SMTP/FTP 那样有跨调用不变的连接参数。</para>
/// </summary>
public sealed class SnowExcelModule : SnowModule;
