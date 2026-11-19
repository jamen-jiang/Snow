using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Module = Rongia.Modularity.Module;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rongia.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 模块化应用:持有模块容器与服务集合,
/// 负责按依赖顺序驱动模块的服务注册阶段、初始化阶段与关闭阶段
/// </summary>
public sealed class Application : IAsyncDisposable, IDisposable
{
    private const double SlowModuleThresholdMilliseconds = 100;

    private readonly List<ModuleRegistrationTiming> _registrationTimings = new();

    private IServiceProvider? _serviceProvider;
    private bool _disposed;
    private double _configureServicesTotalMilliseconds;

    /// <summary>
    /// 本次应用加载的模块容器
    /// </summary>
    public IModuleContainer Modules { get; }

    /// <summary>
    /// 服务集合,与宿主使用的 IServiceCollection 是同一个实例
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 各模块在服务注册阶段的耗时,按执行顺序排列;宿主可用于启动诊断
    /// </summary>
    public IReadOnlyList<ModuleRegistrationTiming> RegistrationTimings => _registrationTimings;

    internal Application(IServiceCollection services, IModuleContainer modules)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
    }

    /// <summary>
    /// 驱动所有模块的服务注册阶段:逐模块做约定扫描,再调用模块的 ConfigureServices
    /// </summary>
    /// <returns>表示注册过程的任务</returns>
    public async Task ConfigureServicesAsync()
    {
        IReadOnlyList<ModuleDescriptor> modules = Modules.Modules;

        ServiceConfigurationContext context = new ServiceConfigurationContext(Services);

        Services.AddSingleton(context);

        // Options 基础设施全局注册一次(对齐 ABP 由 AbpCoreModule 统一 AddOptions 的做法),
        // 这样任何模块都可以直接注入 IOptions<T>, 无需各自补齐; 重复调用幂等。
        Services.AddOptions();

        // 同一程序集只扫描一次: 多个模块同处一个程序集时, 避免重复登记服务描述符
        HashSet<Assembly> scannedAssemblies = new HashSet<Assembly>();
        Stopwatch totalStopwatch = Stopwatch.StartNew();

        foreach (ModuleDescriptor module in modules)
        {
            Stopwatch moduleStopwatch = Stopwatch.StartNew();

            if (scannedAssemblies.Add(module.Assembly))
            {
                Services.AddConventionalRegistrars(module.Assembly);
            }

            double scanMilliseconds = moduleStopwatch.Elapsed.TotalMilliseconds;

            moduleStopwatch.Restart();

            await module.Instance.ConfigureServicesAsync(context);

            ModuleRegistrationTiming timing = new ModuleRegistrationTiming(
                module.Type, scanMilliseconds, moduleStopwatch.Elapsed.TotalMilliseconds);

            _registrationTimings.Add(timing);
        }

        totalStopwatch.Stop();

        _configureServicesTotalMilliseconds = totalStopwatch.Elapsed.TotalMilliseconds;
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

        LogRegistrationTimings(serviceProvider);

        ApplicationInitializationContext context = new ApplicationInitializationContext(serviceProvider);

        IReadOnlyList<ModuleDescriptor> modules = Modules.Modules;

        foreach (ModuleDescriptor module in modules)
        {
            if (module.Instance is IOnApplicationInitialization initialization)
            {
                await initialization.OnApplicationInitializationAsync(context);
            }
        }
    }

    /// <summary>
    /// 输出服务注册阶段的启动诊断:逐模块耗时(调试级)、慢模块告警、总计(信息级)
    /// </summary>
    /// <param name="serviceProvider">容器构建完成后的服务提供者</param>
    private void LogRegistrationTimings(IServiceProvider serviceProvider)
    {
        // 分类名由 ILogger<T> 自动取 typeof(T).FullName, 不写死字符串
        ILogger? logger = serviceProvider.GetService<ILogger<Application>>();

        if (logger is null || _registrationTimings.Count == 0)
        {
            return;
        }

        foreach (ModuleRegistrationTiming timing in _registrationTimings)
        {
            logger.LogDebug(
                "模块 {Module} 注册耗时: 扫描 {ScanMilliseconds:F2} ms, ConfigureServices {ConfigureMilliseconds:F2} ms",
                timing.ModuleType.Name, timing.ScanMilliseconds, timing.ConfigureServicesMilliseconds);

            if (timing.TotalMilliseconds >= SlowModuleThresholdMilliseconds)
            {
                logger.LogWarning(
                    "模块 {Module} 注册偏慢: {TotalMilliseconds:F2} ms(阈值 {Threshold:F0} ms)",
                    timing.ModuleType.Name, timing.TotalMilliseconds, SlowModuleThresholdMilliseconds);
            }
        }

        logger.LogInformation(
            "共 {ModuleCount} 个模块, 服务注册阶段总耗时 {TotalMilliseconds:F2} ms",
            _registrationTimings.Count, _configureServicesTotalMilliseconds);
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

        IReadOnlyList<ModuleDescriptor> modules = Modules.Modules;

        for (int index = modules.Count - 1; index >= 0; index--)
        {
            if (modules[index].Instance is IOnApplicationShutdown shutdown)
            {
                await shutdown.OnApplicationShutdownAsync(context);
            }
        }
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
