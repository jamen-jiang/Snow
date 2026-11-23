namespace Snow.Modularity;

/// <summary>
/// 服务注册后段(对应 ABP 的 IPostConfigureServices): 全部模块的中段跑完之后
/// </summary>
public interface IPostConfigureServices
{
    /// <summary>
    /// 服务注册后段
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    void PostConfigureServices(ServiceConfigurationContext context);

    /// <summary>
    /// 服务注册后段的异步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    Task PostConfigureServicesAsync(ServiceConfigurationContext context);
}
