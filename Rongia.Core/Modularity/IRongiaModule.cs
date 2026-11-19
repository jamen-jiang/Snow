namespace Rongia.Modularity;

/// <summary>
/// 模块契约(对应 ABP 的 IAbpModule):只约束服务注册阶段,
/// 其余阶段由 IPreConfigureServices / IPostConfigureServices / IOn* 系列接口按需实现
/// </summary>
public interface IRongiaModule
{
    /// <summary>
    /// 服务注册阶段的异步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    Task ConfigureServicesAsync(ServiceConfigurationContext context);

    /// <summary>
    /// 服务注册阶段
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    void ConfigureServices(ServiceConfigurationContext context);
}
