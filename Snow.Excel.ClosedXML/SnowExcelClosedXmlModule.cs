using Snow.Modularity;

namespace Snow.Excel.ClosedXML;

/// <summary>
/// Excel ClosedXML 模块: 依赖 <see cref="SnowExcelModule"/>, 用 <see cref="ClosedXmlExcelService"/> 顶替契约包的静默实现
/// <para>顶替靠 <see cref="ClosedXmlExcelService"/> 上的 <c>[TransientService(ReplaceServices = true)]</c> 由约定注册完成,
/// 这里不需要写替换代码。</para>
/// <para>ClosedXML 不需要任何配置节, 本模块只作加载锚点(让本程序集进入约定扫描范围)。</para>
/// </summary>
[DependsOn(typeof(SnowExcelModule))]
public sealed class SnowExcelClosedXmlModule : SnowModule;
