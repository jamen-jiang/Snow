namespace Rongia.Modularity;

/// <summary>
/// 服务注册的后置阶段(对应 ABP 的 IPostConfigureServices):
/// 在全部模块的 ConfigureServices 之后执行,适合放覆盖式注册
/// </summary>
public interface IPostConfigureServices
{
    /// <summary>
    /// 后置注册阶段的异步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    Task PostConfigureServicesAsync(ServiceConfigurationContext context);

    /// <summary>
    /// 后置注册阶段
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    void PostConfigureServices(ServiceConfigurationContext context);
}
