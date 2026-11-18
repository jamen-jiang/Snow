using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 模块化应用契约(对应 ABP 的 IAbpApplication):
/// 持有模块容器与服务集合,负责驱动模块的服务注册与生命周期三阶段
/// </summary>
public interface IApplication : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 服务集合,与宿主使用的 IServiceCollection 是同一个实例
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// 本次应用加载的模块容器
    /// </summary>
    IModuleContainer Modules { get; }

    /// <summary>
    /// 驱动所有模块的服务注册阶段(异步)
    /// </summary>
    /// <returns>表示注册过程的任务</returns>
    Task ConfigureServicesAsync();

    /// <summary>
    /// 驱动所有模块的应用初始化阶段,容器必须已经构建完成(异步)
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    /// <returns>表示初始化过程的任务</returns>
    Task InitializeAsync(IServiceProvider serviceProvider);

    /// <summary>
    /// 驱动所有模块的关闭阶段,按加载顺序的逆序执行(异步)
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>表示关闭过程的任务</returns>
    Task ShutdownAsync(IServiceProvider serviceProvider);

    /// <summary>
    /// 驱动所有模块的服务注册阶段(同步,内部阻塞等待异步版本)
    /// </summary>
    void ConfigureServices();

    /// <summary>
    /// 驱动所有模块的应用初始化阶段(同步,内部阻塞等待异步版本)
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    void Initialize(IServiceProvider serviceProvider);

    /// <summary>
    /// 驱动所有模块的关闭阶段(同步,内部阻塞等待异步版本)
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    void Shutdown(IServiceProvider serviceProvider);
}
