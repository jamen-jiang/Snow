namespace Snow.Modularity;

/// <summary>
/// 单个模块在服务注册阶段的耗时记录,用于启动诊断
/// </summary>
/// <param name="ModuleType">模块类型</param>
/// <param name="ScanMilliseconds">约定扫描耗时(毫秒)</param>
/// <param name="ConfigureServicesMilliseconds">ConfigureServices 耗时(毫秒)</param>
public sealed record ModuleRegistrationTiming(Type ModuleType, double ScanMilliseconds, double ConfigureServicesMilliseconds)
{
    /// <summary>
    /// 合计耗时(毫秒)
    /// </summary>
    public double TotalMilliseconds => ScanMilliseconds + ConfigureServicesMilliseconds;
}
