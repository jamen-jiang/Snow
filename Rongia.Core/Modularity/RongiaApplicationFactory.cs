using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rongia.Modularity;

/// <summary>
/// 模块化应用的创建入口(对应 ABP 的 AbpApplicationFactory):
/// 加载模块、登记容器与应用实例,并立即完成服务注册阶段
/// </summary>
public static class RongiaApplicationFactory
{
    /// <summary>
    /// 加载启动模块及其全部依赖,完成服务注册阶段并返回应用实例
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>已完成服务注册的应用实例</returns>
    public static Task<RongiaApplication> CreateAsync<TStartupModule>(IServiceCollection services)
        where TStartupModule : RongiaModule
    {
        return CreateAsync(services, typeof(TStartupModule));
    }

    /// <summary>
    /// 加载指定启动模块及其全部依赖,完成服务注册阶段并返回应用实例
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns>已完成服务注册的应用实例</returns>
    public static async Task<RongiaApplication> CreateAsync(IServiceCollection services, Type startupModuleType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(startupModuleType);

        ModuleLoadResult loadResult = new ModuleLoader().LoadModules(services, startupModuleType);

        // 加载器已建好 Type → 描述信息 的索引,容器直接复用
        ModuleContainer container = new ModuleContainer(loadResult);
        RongiaApplication application = new RongiaApplication(services, container, startupModuleType);

        services.AddSingleton<IModuleContainer>(container);
        services.AddSingleton<IRongiaApplication>(application);
        services.AddSingleton(application);

        // 内核运行时上下文(租户/客户端/链路标识)与业务模块无关, 由框架直接登记
        services.AddCoreRongiaServices();

        RegisterModuleLifecycle(services);

        await application.ConfigureServicesAsync();

        return application;
    }

    /// <summary>
    /// 登记生命周期贡献者顺序与模块管理器(对应 ABP 的 AbpCoreModule 里对
    /// AbpModuleLifecycleOptions.Contributors 与 IModuleManager 的注册)
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void RegisterModuleLifecycle(IServiceCollection services)
    {
        services.Configure<RongiaModuleLifecycleOptions>(options =>
        {
            options.Contributors.Add(typeof(OnPreApplicationInitializationModuleLifecycleContributor));
            options.Contributors.Add(typeof(OnApplicationInitializationModuleLifecycleContributor));
            options.Contributors.Add(typeof(OnPostApplicationInitializationModuleLifecycleContributor));
            options.Contributors.Add(typeof(OnApplicationShutdownModuleLifecycleContributor));
        });

        services.TryAddTransient<OnPreApplicationInitializationModuleLifecycleContributor>();
        services.TryAddTransient<OnApplicationInitializationModuleLifecycleContributor>();
        services.TryAddTransient<OnPostApplicationInitializationModuleLifecycleContributor>();
        services.TryAddTransient<OnApplicationShutdownModuleLifecycleContributor>();

        services.TryAddSingleton<IModuleManager, ModuleManager>();
    }
}
