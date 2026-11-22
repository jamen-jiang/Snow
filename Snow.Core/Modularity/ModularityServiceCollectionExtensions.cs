using Microsoft.Extensions.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块化的服务集合扩展(对应 ABP 的 ServiceCollectionApplicationExtensions):
/// 用一个启动模块换取整套模块装配
/// </summary>
public static class ModularityServiceCollectionExtensions
{
    /// <summary>
    /// 加载启动模块及其全部依赖,注册模块容器与应用实例,并立即执行服务注册阶段
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块类型</typeparam>
    /// <param name="services">服务集合</param>
    public static void AddApplication<TStartupModule>(this IServiceCollection services)
        where TStartupModule : SnowModule
    {
        services.AddApplication(typeof(TStartupModule));
    }

    /// <summary>
    /// 加载指定启动模块及其全部依赖,注册模块容器与应用实例,并立即执行服务注册阶段
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="startupModuleType">启动模块类型</param>
    public static void AddApplication(this IServiceCollection services, Type startupModuleType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(startupModuleType);

        IReadOnlyList<SnowModuleDescriptor> modules = ModuleLoader.LoadModules(services, startupModuleType);

        ModuleContainer container = new(modules);
        SnowApplication application = new(services, container);

        services.AddSingleton<IModuleContainer>(container);
        services.AddSingleton(application);

        application.ConfigureServicesAsync().GetAwaiter().GetResult();
    }
}
