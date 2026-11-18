using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rongia.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 模块化应用:持有模块容器与服务集合,
/// 负责按依赖顺序驱动模块的服务注册三阶段与初始化三阶段
/// <para>同步方法只是异步版本的阻塞包装,宿主优先用异步版本。</para>
/// </summary>
public sealed class Application : IApplication, IAsyncDisposable, IDisposable
{
    private IServiceProvider? _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// 本次应用加载的模块容器
    /// </summary>
    public IModuleContainer Modules { get; }

    /// <summary>
    /// 服务集合,与宿主使用的 IServiceCollection 是同一个实例
    /// </summary>
    public IServiceCollection Services { get; }

    internal Application(IServiceCollection services, IModuleContainer modules)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
    }

    /// <summary>
    /// 驱动所有模块的服务注册阶段:
    /// 先跑全部模块的 PreConfigureServices,再逐模块做约定扫描与 ConfigureServices,
    /// 最后跑全部模块的 PostConfigureServices
    /// </summary>
    /// <returns>表示注册过程的任务</returns>
    public async Task ConfigureServicesAsync()
    {
        IReadOnlyList<IModuleDescriptor> modules = Modules.Modules;

        ServiceConfigurationContext context = new ServiceConfigurationContext(Services);

        Services.AddSingleton(context);

        foreach (IModuleDescriptor module in modules)
        {
            if (module.Instance is Module moduleBase)
            {
                moduleBase.PreConfigureServices(context);
            }
        }

        foreach (IModuleDescriptor module in modules)
        {
            Services.AddConventionalRegistrars(module.Assembly);

            await module.Instance.ConfigureServicesAsync(context);
        }

        foreach (IModuleDescriptor module in modules)
        {
            if (module.Instance is Module moduleBase)
            {
                moduleBase.PostConfigureServices(context);
            }
        }
    }

    /// <summary>
    /// 驱动所有模块的应用初始化阶段,容器必须已经构建完成
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    /// <returns>表示初始化过程的任务</returns>
    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _serviceProvider = serviceProvider;

        ApplicationInitializationContext context = new ApplicationInitializationContext(serviceProvider);

        IReadOnlyList<IModuleDescriptor> modules = Modules.Modules;

        foreach (IModuleDescriptor module in modules)
        {
            if (module.Instance is IOnPreApplicationInitialization initialization)
            {
                await initialization.OnPreApplicationInitializationAsync(context);
            }
        }

        foreach (IModuleDescriptor module in modules)
        {
            if (module.Instance is IOnApplicationInitialization initialization)
            {
                await initialization.OnApplicationInitializationAsync(context);
            }
        }

        foreach (IModuleDescriptor module in modules)
        {
            if (module.Instance is IOnPostApplicationInitialization initialization)
            {
                await initialization.OnPostApplicationInitializationAsync(context);
            }
        }
    }

    /// <summary>
    /// 驱动所有模块的关闭阶段,按加载顺序的逆序执行
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>表示关闭过程的任务</returns>
    public async Task ShutdownAsync(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        ApplicationShutdownContext context = new ApplicationShutdownContext(serviceProvider);

        IReadOnlyList<IModuleDescriptor> modules = Modules.Modules;

        for (int index = modules.Count - 1; index >= 0; index--)
        {
            if (modules[index].Instance is IOnApplicationShutdown shutdown)
            {
                await shutdown.OnApplicationShutdownAsync(context);
            }
        }
    }

    /// <summary>
    /// 驱动所有模块的服务注册阶段(同步)
    /// </summary>
    public void ConfigureServices()
    {
        ConfigureServicesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 驱动所有模块的应用初始化阶段(同步)
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    public void Initialize(IServiceProvider serviceProvider)
    {
        InitializeAsync(serviceProvider).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 驱动所有模块的关闭阶段(同步)
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public void Shutdown(IServiceProvider serviceProvider)
    {
        ShutdownAsync(serviceProvider).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 关闭模块并释放服务提供者(异步)
    /// </summary>
    /// <returns>表示释放过程的任务</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_serviceProvider != null)
        {
            await ShutdownAsync(_serviceProvider);

            if (_serviceProvider is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// 关闭模块并释放服务提供者(同步)
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
