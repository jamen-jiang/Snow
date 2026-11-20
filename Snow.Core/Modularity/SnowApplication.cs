using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块化应用(对应 ABP 的 AbpApplicationBase + AbpApplicationWithExternalServiceProvider):
/// 持有模块容器与服务集合,按依赖顺序驱动模块的三个注册阶段与三个初始化阶段,
/// 关闭阶段交给 <see cref="IModuleManager"/> 逆序执行
/// </summary>
public sealed class SnowApplication : ISnowApplication
{
    private const double SlowModuleThresholdMilliseconds = 100;

    private readonly List<ModuleRegistrationTiming> _registrationTimings = new();

    private IServiceProvider? _serviceProvider;
    private bool _disposed;
    private bool _configuredServices;
    private double _configureServicesTotalMilliseconds;

    /// <inheritdoc />
    public Type StartupModuleType { get; }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IReadOnlyList<SnowModuleDescriptor> Modules { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<Type, SnowModuleDescriptor> ModulesByType { get; }

    /// <inheritdoc />
    /// <exception cref="SnowInitializationException">初始化之前访问</exception>
    public IServiceProvider ServiceProvider => _serviceProvider
        ?? throw new SnowInitializationException("应用尚未初始化, 请先调用 " + nameof(InitializeAsync) + "。");

    /// <inheritdoc />
    public SnowModuleDescriptor? Find(Type moduleType)
    {
        ArgumentNullException.ThrowIfNull(moduleType);

        return ModulesByType.TryGetValue(moduleType, out SnowModuleDescriptor? module) ? module : null;
    }

    /// <summary>
    /// 各模块在服务注册阶段的耗时,按执行顺序排列;宿主可用于启动诊断
    /// </summary>
    public IReadOnlyList<ModuleRegistrationTiming> RegistrationTimings => _registrationTimings;

    internal SnowApplication(IServiceCollection services, IModuleContainer modules, Type startupModuleType)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        ArgumentNullException.ThrowIfNull(modules);
        StartupModuleType = startupModuleType ?? throw new ArgumentNullException(nameof(startupModuleType));

        Modules = modules.Modules;
        ModulesByType = modules.ModulesByType;
    }

    /// <inheritdoc />
    public async Task ConfigureServicesAsync()
    {
        CheckMultipleConfigureServices();

        ServiceConfigurationContext context = new ServiceConfigurationContext(Services);

        Services.AddSingleton(context);

        // Options 基础设施全局注册一次(对齐 ABP 由 AbpCoreModule 统一 AddOptions 的做法),
        // 这样任何模块都可以直接注入 IOptions<T>, 无需各自补齐; 重复调用幂等。
        Services.AddOptions();

        AttachContext(context);

        // 1. 前置注册: 全部模块先跑一遍
        foreach (SnowModuleDescriptor module in Modules)
        {
            if (module.Instance is IPreConfigureServices preConfigureServices)
            {
                await preConfigureServices.PreConfigureServicesAsync(context);
            }
        }

        // 2. 注册: 逐模块做约定扫描, 再调用模块的 ConfigureServices
        HashSet<Assembly> scannedAssemblies = new HashSet<Assembly>();
        Stopwatch totalStopwatch = Stopwatch.StartNew();

        foreach (SnowModuleDescriptor module in Modules)
        {
            Stopwatch moduleStopwatch = Stopwatch.StartNew();

            if (!module.Instance.SkipAutoServiceRegistration)
            {
                foreach (Assembly assembly in module.AllAssemblies)
                {
                    if (scannedAssemblies.Add(assembly))
                    {
                        Services.AddConventionalRegistrars(assembly);
                    }
                }
            }

            double scanMilliseconds = moduleStopwatch.Elapsed.TotalMilliseconds;

            moduleStopwatch.Restart();

            await module.Instance.ConfigureServicesAsync(context);

            _registrationTimings.Add(new ModuleRegistrationTiming(
                module.Type, scanMilliseconds, moduleStopwatch.Elapsed.TotalMilliseconds));
        }

        totalStopwatch.Stop();
        _configureServicesTotalMilliseconds = totalStopwatch.Elapsed.TotalMilliseconds;

        // 3. 后置注册: 全部模块再跑一遍, 适合覆盖式注册
        foreach (SnowModuleDescriptor module in Modules)
        {
            if (module.Instance is IPostConfigureServices postConfigureServices)
            {
                await postConfigureServices.PostConfigureServicesAsync(context);
            }
        }

        DetachContext();

        _configuredServices = true;
    }

    /// <inheritdoc />
    public void ConfigureServices()
    {
        CheckMultipleConfigureServices();

        ServiceConfigurationContext context = new ServiceConfigurationContext(Services);

        Services.AddSingleton(context);
        Services.AddOptions();

        AttachContext(context);

        foreach (SnowModuleDescriptor module in Modules)
        {
            if (module.Instance is IPreConfigureServices preConfigureServices)
            {
                preConfigureServices.PreConfigureServices(context);
            }
        }

        HashSet<Assembly> scannedAssemblies = new HashSet<Assembly>();
        Stopwatch totalStopwatch = Stopwatch.StartNew();

        foreach (SnowModuleDescriptor module in Modules)
        {
            Stopwatch moduleStopwatch = Stopwatch.StartNew();

            if (!module.Instance.SkipAutoServiceRegistration)
            {
                foreach (Assembly assembly in module.AllAssemblies)
                {
                    if (scannedAssemblies.Add(assembly))
                    {
                        Services.AddConventionalRegistrars(assembly);
                    }
                }
            }

            double scanMilliseconds = moduleStopwatch.Elapsed.TotalMilliseconds;

            moduleStopwatch.Restart();

            module.Instance.ConfigureServices(context);

            _registrationTimings.Add(new ModuleRegistrationTiming(
                module.Type, scanMilliseconds, moduleStopwatch.Elapsed.TotalMilliseconds));
        }

        totalStopwatch.Stop();
        _configureServicesTotalMilliseconds = totalStopwatch.Elapsed.TotalMilliseconds;

        foreach (SnowModuleDescriptor module in Modules)
        {
            if (module.Instance is IPostConfigureServices postConfigureServices)
            {
                postConfigureServices.PostConfigureServices(context);
            }
        }

        DetachContext();

        _configuredServices = true;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        SetServiceProvider(serviceProvider);

        LogRegistrationTimings(serviceProvider);

        ApplicationInitializationContext context = new ApplicationInitializationContext(serviceProvider);

        await GetModuleManager(serviceProvider).InitializeModulesAsync(context);
    }

    /// <inheritdoc />
    public void Initialize(IServiceProvider serviceProvider)
    {
        SetServiceProvider(serviceProvider);

        LogRegistrationTimings(serviceProvider);

        ApplicationInitializationContext context = new ApplicationInitializationContext(serviceProvider);

        GetModuleManager(serviceProvider).InitializeModules(context);
    }

    /// <inheritdoc />
    public async Task ShutdownAsync()
    {
        if (_serviceProvider is null)
        {
            return;
        }

        ApplicationShutdownContext context = new ApplicationShutdownContext(_serviceProvider);

        await GetModuleManager(_serviceProvider).ShutdownModulesAsync(context);
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        if (_serviceProvider is null)
        {
            return;
        }

        ApplicationShutdownContext context = new ApplicationShutdownContext(_serviceProvider);

        GetModuleManager(_serviceProvider).ShutdownModules(context);
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

        if (_serviceProvider is not null)
        {
            await ShutdownAsync();

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

    private void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        if (_serviceProvider is not null)
        {
            if (!ReferenceEquals(_serviceProvider, serviceProvider))
            {
                throw new SnowInitializationException("服务提供者此前已被设置为另一个实例, 不能重复设置。");
            }

            return;
        }

        _serviceProvider = serviceProvider;
    }

    private void CheckMultipleConfigureServices()
    {
        if (_configuredServices)
        {
            throw new SnowInitializationException("服务注册阶段已经执行过, 不能重复执行。");
        }
    }

    private void AttachContext(ServiceConfigurationContext context)
    {
        foreach (SnowModuleDescriptor module in Modules)
        {
            module.Instance.ServiceConfigurationContext = context;
        }
    }

    private void DetachContext()
    {
        foreach (SnowModuleDescriptor module in Modules)
        {
            module.Instance.ServiceConfigurationContext = null;
        }
    }

    private IModuleManager GetModuleManager(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IModuleManager>();
    }

    /// <summary>
    /// 输出服务注册阶段的启动诊断:逐模块耗时(调试级)、慢模块告警、总计(信息级)
    /// </summary>
    /// <param name="serviceProvider">容器构建完成后的服务提供者</param>
    private void LogRegistrationTimings(IServiceProvider serviceProvider)
    {
        // 分类名由 ILogger<T> 自动取 typeof(T).FullName, 不写死字符串
        ILogger? logger = serviceProvider.GetService<ILogger<SnowApplication>>();

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
}
