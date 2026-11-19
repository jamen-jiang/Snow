using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 模块化应用的创建入口(对应 ABP 的 AbpApplicationFactory):
/// 加载模块、登记容器与应用实例,并立即完成服务注册阶段
/// </summary>
public static class ApplicationFactory
{
    /// <summary>
    /// 加载启动模块及其全部依赖,完成服务注册阶段并返回应用实例
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>已完成服务注册的应用实例</returns>
    public static Task<Application> CreateAsync<TStartupModule>(IServiceCollection services)
        where TStartupModule : Module
    {
        return CreateAsync(services, typeof(TStartupModule));
    }

    /// <summary>
    /// 加载指定启动模块及其全部依赖,完成服务注册阶段并返回应用实例
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns>已完成服务注册的应用实例</returns>
    public static async Task<Application> CreateAsync(IServiceCollection services, Type startupModuleType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(startupModuleType);

        ModuleLoadResult loadResult = new ModuleLoader().LoadModules(services, startupModuleType);

        // 加载器已建好 Type → 描述信息 的索引,容器直接复用
        ModuleContainer container = new ModuleContainer(loadResult);
        Application application = new Application(services, container);

        services.AddSingleton<IModuleContainer>(container);
        services.AddSingleton(application);

        await application.ConfigureServicesAsync();

        return application;
    }
}
