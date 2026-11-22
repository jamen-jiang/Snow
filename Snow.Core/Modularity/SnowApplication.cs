using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块化应用(对应 ABP 的 AbpApplicationBase + AbpApplicationWithExternalServiceProvider):
/// 持有模块容器与服务集合,按依赖顺序驱动模块的服务注册与应用初始化两个阶段
/// </summary>
public sealed class SnowApplication
{
    private IServiceProvider? _serviceProvider;
    private bool _configuredServices;

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IReadOnlyList<SnowModuleDescriptor> Modules { get; }

    /// <inheritdoc />
    /// <exception cref="SnowInitializationException">初始化之前访问</exception>
    public IServiceProvider ServiceProvider => _serviceProvider
        ?? throw new SnowInitializationException("应用尚未初始化, 请先调用 " + nameof(InitializeAsync) + "。");

    internal SnowApplication(IServiceCollection services, IModuleContainer modules)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        ArgumentNullException.ThrowIfNull(modules);

        Modules = modules.Modules;
    }

    /// <inheritdoc />
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

        // 逐模块只做约定扫描并收集候选, 显式登记(ConfigureServices)全部落地后再统一写入约定登记,
        // 这样宿主与模块的显式登记稳定排在约定登记之前, TryAdd / Add / Replace 都能顶掉默认实现
        HashSet<Assembly> scannedAssemblies = new HashSet<Assembly>();
        List<ServiceCollectionExtensions.ConventionalRegistration> conventionalCandidates = new();

        foreach (SnowModuleDescriptor module in Modules)
        {
            if (!module.Instance.SkipAutoServiceRegistration)
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

        // 约定登记统一写入: 此时集合里已经有所有显式登记(宿主与模块的)
        ServiceCollectionExtensions.ApplyConventionalRegistrations(Services, conventionalCandidates);

        DetachContext();

        _configuredServices = true;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(IServiceProvider serviceProvider)
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

        ApplicationInitializationContext context = new ApplicationInitializationContext(serviceProvider);

        foreach (SnowModuleDescriptor module in Modules)
        {
            try
            {
                await module.Instance.OnApplicationInitializationAsync(context);
            }
            catch (Exception exception)
            {
                throw new SnowInitializationException(
                    "模块 " + module.Type.AssemblyQualifiedName + " 在应用初始化阶段抛出异常: " + exception.Message,
                    exception);
            }
        }

        serviceProvider.GetService<ILogger<SnowApplication>>()?.LogInformation("全部模块初始化完成");
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
}