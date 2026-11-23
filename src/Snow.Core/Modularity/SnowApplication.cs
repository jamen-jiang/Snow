using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块化应用(对应 ABP 的 AbpApplicationBase + AbpApplicationWithExternalServiceProvider):
/// 持有模块容器与服务集合,按依赖顺序驱动模块的三个初始化阶段与关停阶段。
/// <para>服务注册阶段一段三趟: 全部模块的 <c>PreConfigureServices</c> → 全部模块的 <c>ConfigureServices</c>
/// → 全部模块的 <c>PostConfigureServices</c>(与 ABP 一致, 不是"一个模块跑完三段再下一个")。</para>
/// <para>应用初始化阶段两段: 第一段 <see cref="InitializeModulesAsync"/> 跑各模块的 <c>OnApplicationInitialization</c>;
/// 收尾段 <see cref="PostInitializeModulesAsync"/> 跑各模块的 <c>OnPostApplicationInitialization</c>。
/// 两段之前还有 <see cref="PreInitializeModulesAsync"/>(跑 <c>OnPreApplicationInitialization</c>)。
/// Web 宿主在 <c>Program.cs</c> 里用 <c>await app.InitializeApplicationAsync()</c> 一次跑完三段(对齐 ABP),
/// 启动模块在 <c>OnApplicationInitialization</c> 里手写的整条管道因此在 <c>app.Run()</c> 之前定型。</para>
/// </summary>
public sealed class SnowApplication
{
    private IServiceProvider? _serviceProvider;
    private bool _configuredServices;
    private bool _preInitializedModules;
    private bool _initializedModules;
    private bool _postInitializedModules;
    private bool _shutdowned;

    /// <summary>
    /// 宿主使用的服务集合
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 按加载顺序排列的模块, 被依赖的在前
    /// </summary>
    public IReadOnlyList<SnowModuleDescriptor> Modules { get; }

    /// <summary>
    /// 根服务提供者
    /// </summary>
    /// <exception cref="SnowInitializationException">初始化之前访问</exception>
    public IServiceProvider ServiceProvider => _serviceProvider
        ?? throw new SnowInitializationException("应用尚未初始化, 请先调用 " + nameof(InitializeAsync) + "。");

    internal SnowApplication(IServiceCollection services, IModuleContainer modules)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        ArgumentNullException.ThrowIfNull(modules);

        Modules = modules.Modules;
    }

    /// <summary>
    /// 服务注册阶段: 逐趟跑全部模块的前段、中段、后段
    /// </summary>
    /// <exception cref="SnowInitializationException">重复调用</exception>
    public async Task ConfigureServicesAsync()
    {
        if (_configuredServices)
        {
            throw new SnowInitializationException("服务注册阶段已经执行过, 不能重复执行。");
        }

        ServiceConfigurationContext context = new ServiceConfigurationContext(Services);

        Services.AddSingleton(context);

        // Options 基础设施全局注册一次(对齐 ABP 由 AbpCoreModule 统一 AddOptions 的做法),
        // 这样任何模块都可以直接注入 IOptions<T>, 无需各自补齐; 重复调用幂等。
        Services.AddOptions();

        AttachContext(context);

        // 第一趟: 全部模块的前段。谁都还没做正式登记, 适合放"必须在别人之前就位"的占位
        foreach (SnowModuleDescriptor module in Modules)
        {
            if (module.Instance is IPreConfigureServices target)
            {
                await target.PreConfigureServicesAsync(context);
            }
        }

        // 第二趟: 约定扫描 + 全部模块的中段。显式登记全部落地后再统一写入约定登记,
        // 这样宿主与模块的显式登记稳定排在约定登记之前, TryAdd / Add / Replace 都能顶掉默认实现
        HashSet<Assembly> scannedAssemblies = new HashSet<Assembly>();
        List<ServiceCollectionExtensions.ConventionalRegistration> conventionalCandidates = new();

        foreach (SnowModuleDescriptor module in Modules)
        {
            // 只认基类: 跳过约定扫描是 SnowModule 上的开关, 只实现接口的模块没有这个概念(默认不跳)
            if (module.Instance is SnowModule snowModule && !snowModule.SkipAutoServiceRegistration)
            {
                foreach (Assembly assembly in module.AllAssemblies)
                {
                    if (scannedAssemblies.Add(assembly))
                    {
                        conventionalCandidates.AddRange(ServiceCollectionExtensions.CollectCandidates(assembly));
                    }
                }
            }

            await module.Instance.ConfigureServicesAsync(context);
        }

        // 第三趟: 全部模块的后段。此时所有中段登记都在, 适合替换默认实现或补装饰器
        foreach (SnowModuleDescriptor module in Modules)
        {
            if (module.Instance is IPostConfigureServices target)
            {
                await target.PostConfigureServicesAsync(context);
            }
        }

        // 约定登记统一写入: 此时集合里已经有所有显式登记(宿主与模块的)
        ServiceCollectionExtensions.ApplyConventionalRegistrations(Services, conventionalCandidates);

        DetachContext();

        _configuredServices = true;

        TryToSetEnvironment(Services);
    }

    /// <summary>
    /// 环境名兜底: 全部模块跑完仍没人填, 就按生产环境处理
    /// <para>对应 ABP 的 <c>AbpApplicationBase.TryToSetEnvironment</c>: 同一处、同样在 PostConfigureServices 之后。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void TryToSetEnvironment(IServiceCollection services)
    {
        // 只用 ImplementationInstance 查: 这里还没建容器, 不能为了一个字符串去构建临时容器
        ISnowHostEnvironment? hostEnvironment = services.GetSingletonInstanceOrNull<ISnowHostEnvironment>();
        if (hostEnvironment is null)
        {
            return;
        }

        if (hostEnvironment.EnvironmentName.IsNullOrWhiteSpace())
        {
            hostEnvironment.EnvironmentName = SnowEnvironments.Production;
        }
    }

    /// <summary>
    /// 跑完整个应用初始化: 预初始化 → 第一段 → 收尾段
    /// <para>非 Web 宿主直接用它一次跑完; Web 宿主由 <c>app.InitializeApplicationAsync()</c> 代跑, 那个扩展方法按同样顺序调下面三个方法。</para>
    /// </summary>
    /// <param name="serviceProvider">根服务提供者</param>
    /// <returns>表示初始化过程的任务</returns>
    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        await PreInitializeModulesAsync(serviceProvider);
        await InitializeModulesAsync(serviceProvider);
        await PostInitializeModulesAsync();
    }

    /// <summary>
    /// 应用预初始化阶段: 逐个模块执行 <see cref="SnowModule.OnPreApplicationInitializationAsync"/>
    /// <para>容器已构建、宿主还没开始写管道; 模块在这里挂的中间件同样落在最外层。
    /// 幂等: 已经用同一个服务提供者跑过就直接返回; 换一个服务提供者抛异常。</para>
    /// </summary>
    /// <param name="serviceProvider">根服务提供者</param>
    /// <returns>表示初始化过程的任务</returns>
    public async Task PreInitializeModulesAsync(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        SetServiceProvider(serviceProvider);

        if (_preInitializedModules)
        {
            return;
        }

        _preInitializedModules = true;

        await RunPhaseAsync(
            serviceProvider,
            "应用预初始化阶段",
            (module, context) => module is IOnPreApplicationInitialization target
                ? target.OnPreApplicationInitializationAsync(context)
                : Task.CompletedTask);
    }

    /// <summary>
    /// 应用初始化第一段: 逐个模块执行 <see cref="SnowModule.OnApplicationInitializationAsync"/>
    /// <para>幂等: 已经用同一个服务提供者跑过就直接返回; 换一个服务提供者抛异常。
    /// 预初始化阶段允许跳过(测试与非 Web 场景可以直接从这里开始), Web 宿主不会跳。</para>
    /// </summary>
    /// <param name="serviceProvider">根服务提供者</param>
    /// <returns>表示初始化过程的任务</returns>
    public async Task InitializeModulesAsync(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        SetServiceProvider(serviceProvider);

        if (_initializedModules)
        {
            return;
        }

        _initializedModules = true;

        await RunPhaseAsync(
            serviceProvider,
            "应用初始化阶段",
            (module, context) => module is IOnApplicationInitialization target
                ? target.OnApplicationInitializationAsync(context)
                : Task.CompletedTask);

        serviceProvider.GetService<ILogger<SnowApplication>>()?.LogInformation("全部模块的应用初始化完成");
    }

    /// <summary>
    /// 应用初始化收尾段: 逐个模块执行 <see cref="SnowModule.OnPostApplicationInitializationAsync"/>
    /// <para>必须在第一段之后调用; Web 宿主的 <c>InitializeApplicationAsync()</c> 紧跟第一段调它,
    /// 模块在这一段挂的中间件因此落在宿主手写的整条管道之后。重复调用幂等。</para>
    /// </summary>
    /// <returns>表示初始化过程的任务</returns>
    public async Task PostInitializeModulesAsync()
    {
        if (!_initializedModules)
        {
            throw new SnowInitializationException(
                "应用初始化第一段还没跑, 不能直接跑收尾阶段: 先调用 " + nameof(InitializeModulesAsync) + "。");
        }

        if (_postInitializedModules)
        {
            return;
        }

        _postInitializedModules = true;

        await RunPhaseAsync(
            ServiceProvider,
            "应用初始化收尾阶段",
            (module, context) => module is IOnPostApplicationInitialization target
                ? target.OnPostApplicationInitializationAsync(context)
                : Task.CompletedTask);
    }

    /// <summary>
    /// 关停应用: 逐个模块执行 <see cref="SnowModule.OnApplicationShutdownAsync"/>, 关停里的服务解析走独立作用域
    /// <para>Web 宿主由 <c>app.InitializeApplicationAsync()</c> 挂在 <c>IHostApplicationLifetime.ApplicationStopping</c> 上, 宿主不用自己调。重复调用幂等。</para>
    /// </summary>
    /// <returns>表示关停过程的任务</returns>
    public async Task ShutdownAsync()
    {
        if (_shutdowned)
        {
            return;
        }

        _shutdowned = true;

        IServiceProvider serviceProvider = ServiceProvider;

        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationShutdownContext context = new ApplicationShutdownContext(scope.ServiceProvider);

        foreach (SnowModuleDescriptor module in Modules)
        {
            try
            {
                if (module.Instance is IOnApplicationShutdown target)
                {
                    await target.OnApplicationShutdownAsync(context);
                }
            }
            catch (Exception exception)
            {
                throw new SnowInitializationException(
                    "模块 " + module.Type.AssemblyQualifiedName + " 在应用关停阶段抛出异常: " + exception.Message,
                    exception);
            }
        }

        serviceProvider.GetService<ILogger<SnowApplication>>()?.LogInformation("全部模块的应用关停完成");
    }

    /// <summary>
    /// 关停应用(同步版本)
    /// </summary>
    public void Shutdown()
    {
        ShutdownAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 设置根服务提供者: 只允许设置一次, 换实例抛异常
    /// </summary>
    /// <param name="serviceProvider">根服务提供者</param>
    private void SetServiceProvider(IServiceProvider serviceProvider)
    {
        if (_serviceProvider is null)
        {
            _serviceProvider = serviceProvider;
            return;
        }

        if (!ReferenceEquals(_serviceProvider, serviceProvider))
        {
            throw new SnowInitializationException("服务提供者此前已被设置为另一个实例, 不能重复设置。");
        }
    }

    /// <summary>
    /// 逐个模块执行某个初始化阶段, 任一模块抛异常都带上模块名重新抛
    /// </summary>
    /// <param name="serviceProvider">根服务提供者</param>
    /// <param name="phase">阶段名(只用于异常文案)</param>
    /// <param name="invoke">要执行的阶段方法</param>
    /// <returns>表示初始化过程的任务</returns>
    private async Task RunPhaseAsync(
        IServiceProvider serviceProvider,
        string phase,
        Func<ISnowModule, ApplicationInitializationContext, Task> invoke)
    {
        ApplicationInitializationContext context = new ApplicationInitializationContext(serviceProvider);

        foreach (SnowModuleDescriptor module in Modules)
        {
            try
            {
                await invoke(module.Instance, context);
            }
            catch (Exception exception)
            {
                throw new SnowInitializationException(
                    "模块 " + module.Type.AssemblyQualifiedName + " 在" + phase + "抛出异常: " + exception.Message,
                    exception);
            }
        }
    }

    private void AttachContext(ServiceConfigurationContext context)
    {
        foreach (SnowModuleDescriptor module in Modules)
        {
            if (module.Instance is SnowModule snowModule)
            {
                snowModule.ServiceConfigurationContext = context;
            }
        }
    }

    private void DetachContext()
    {
        foreach (SnowModuleDescriptor module in Modules)
        {
            if (module.Instance is SnowModule snowModule)
            {
                snowModule.ServiceConfigurationContext = null;
            }
        }
    }
}
