using System;
using System.Threading.Tasks;
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
    public static async Task<IApplication> CreateAsync<TStartupModule>(IServiceCollection services)
        where TStartupModule : IModule
    {
        return await CreateAsync(services, typeof(TStartupModule));
    }

    /// <summary>
    /// 加载指定启动模块及其全部依赖,完成服务注册阶段并返回应用实例
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns>已完成服务注册的应用实例</returns>
    public static async Task<IApplication> CreateAsync(IServiceCollection services, Type startupModuleType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(startupModuleType);

        IModuleLoader loader = new ModuleLoader();
        IModuleDescriptor[] modules = loader.LoadModules(services, startupModuleType);

        ModuleContainer container = new ModuleContainer(modules);
        Application application = new Application(services, container);

        services.AddSingleton<IModuleContainer>(container);
        services.AddSingleton(application);
        services.AddSingleton<IApplication>(application);

        await application.ConfigureServicesAsync();

        return application;
    }
}
