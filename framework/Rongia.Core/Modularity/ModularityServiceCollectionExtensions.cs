using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 模块化的服务集合扩展:用一个启动模块换取整套模块装配
/// </summary>
public static class ModularityServiceCollectionExtensions
{
    /// <summary>
    /// 加载启动模块及其全部依赖,注册模块容器与应用实例,并立即执行服务注册阶段
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>模块化应用实例,用于后续初始化与关闭</returns>
    public static Application AddApplication<TStartupModule>(this IServiceCollection services)
        where TStartupModule : Module
    {
        return services.AddApplication(typeof(TStartupModule));
    }

    /// <summary>
    /// 加载指定启动模块及其全部依赖,注册模块容器与应用实例,并立即执行服务注册阶段
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns>模块化应用实例,用于后续初始化与关闭</returns>
    public static Application AddApplication(this IServiceCollection services, Type startupModuleType)
    {
        ArgumentNullException.ThrowIfNull(services);

        ModuleLoader loader = new ModuleLoader();
        IReadOnlyList<ModuleDescriptor> modules = loader.LoadModules(startupModuleType);

        ModuleContainer container = new ModuleContainer(modules);
        Application application = new Application(services, container);

        services.AddSingleton<IModuleContainer>(container);
        services.AddSingleton(application);

        application.ConfigureServices();

        return application;
    }
}