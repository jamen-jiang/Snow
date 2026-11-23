namespace Snow.Modularity;

/// <summary>
/// 服务注册前段(对应 ABP 的 IPreConfigureServices): 全部模块的第一趟, 谁都还没做正式登记
/// </summary>
public interface IPreConfigureServices
{
    /// <summary>
    /// 服务注册前段
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    void PreConfigureServices(ServiceConfigurationContext context);

    /// <summary>
    /// 服务注册前段的异步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    Task PreConfigureServicesAsync(ServiceConfigurationContext context);
}
