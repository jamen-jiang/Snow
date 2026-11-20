using Microsoft.Extensions.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块化应用契约(对应 ABP 的 IAbpApplication):同时是模块容器,
/// 对外暴露服务集合、服务提供者与各阶段的驱动方法
/// </summary>
public interface ISnowApplication : IModuleContainer, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 启动(入口)模块类型
    /// </summary>
    Type StartupModuleType { get; }

    /// <summary>
    /// 全部服务注册,初始化完成后不应再往里加服务
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// 应用使用的根服务提供者,初始化之前不可用
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 驱动全部模块的服务注册阶段(前置 / 注册 / 后置)
    /// </summary>
    /// <returns>表示注册过程的任务</returns>
    Task ConfigureServicesAsync();

    /// <summary>
    /// 驱动全部模块的服务注册阶段(同步)
    /// </summary>
    void ConfigureServices();

    /// <summary>
    /// 驱动全部模块的初始化阶段
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    /// <returns>表示初始化过程的任务</returns>
    Task InitializeAsync(IServiceProvider serviceProvider);

    /// <summary>
    /// 驱动全部模块的初始化阶段(同步)
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    void Initialize(IServiceProvider serviceProvider);

    /// <summary>
    /// 驱动全部模块的关闭阶段
    /// </summary>
    /// <returns>表示关闭过程的任务</returns>
    Task ShutdownAsync();

    /// <summary>
    /// 驱动全部模块的关闭阶段(同步)
    /// </summary>
    void Shutdown();
}
