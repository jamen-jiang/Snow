namespace Rongia.Modularity;

/// <summary>
/// 服务注册的前置阶段(对应 ABP 的 IPreConfigureServices):
/// 在全部模块的 ConfigureServices 之前执行,适合放"注册前"的约定配置
/// </summary>
public interface IPreConfigureServices
{
    /// <summary>
    /// 前置注册阶段的异步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    Task PreConfigureServicesAsync(ServiceConfigurationContext context);

    /// <summary>
    /// 前置注册阶段
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    void PreConfigureServices(ServiceConfigurationContext context);
}
