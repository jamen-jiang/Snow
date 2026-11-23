using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Snow.Modularity;

/// <summary>
/// 模块化的服务集合扩展(对应 ABP 的 ServiceCollectionApplicationExtensions):
/// 用一个启动模块换取整套模块装配
/// </summary>
public static class ModularityServiceCollectionExtensions
{
    /// <summary>
    /// 加载启动模块及其全部依赖, 注册模块容器与应用实例, 并立即执行服务注册阶段
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="optionsAction">应用创建选项; 可在这里指定环境名</param>
    /// <returns>创建好的应用实例</returns>
    public static SnowApplication AddApplication<TStartupModule>(
        this IServiceCollection services,
        Action<SnowApplicationCreationOptions>? optionsAction = null)
        where TStartupModule : ISnowModule
    {
        return services.AddApplicationAsync<TStartupModule>(optionsAction).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 加载指定启动模块及其全部依赖, 注册模块容器与应用实例, 并立即执行服务注册阶段
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <param name="optionsAction">应用创建选项; 可在这里指定环境名</param>
    /// <returns>创建好的应用实例</returns>
    public static SnowApplication AddApplication(
        this IServiceCollection services,
        Type startupModuleType,
        Action<SnowApplicationCreationOptions>? optionsAction = null)
    {
        return services.AddApplicationAsync(startupModuleType, optionsAction).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 异步版 <see cref="AddApplication{TStartupModule}"/>: 模块的 <c>ConfigureServices</c> 可以是真异步
    /// <para>与 ABP 一样两种入口并存: 同步版内部就是同步等待这一份, 不存在两套实现。</para>
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="optionsAction">应用创建选项; 可在这里指定环境名</param>
    /// <returns>创建好的应用实例</returns>
    public static Task<SnowApplication> AddApplicationAsync<TStartupModule>(
        this IServiceCollection services,
        Action<SnowApplicationCreationOptions>? optionsAction = null)
        where TStartupModule : ISnowModule
    {
        return services.AddApplicationAsync(typeof(TStartupModule), optionsAction);
    }

    /// <summary>
    /// 异步版 <see cref="AddApplication(IServiceCollection, Type, Action{SnowApplicationCreationOptions})"/>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <param name="optionsAction">应用创建选项; 可在这里指定环境名</param>
    /// <returns>创建好的应用实例</returns>
    public static async Task<SnowApplication> AddApplicationAsync(
        this IServiceCollection services,
        Type startupModuleType,
        Action<SnowApplicationCreationOptions>? optionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(startupModuleType);

        SnowApplicationCreationOptions options = new(services);
        optionsAction?.Invoke(options);

        // 环境名要在模块装配之前就位: 模块在 PreConfigureServices / ConfigureServices 里就可能问「现在是什么环境」。
        // 对应 ABP 在 AbpApplicationBase 构造器里 services.AddSingleton<IAbpHostEnvironment>(new AbpHostEnvironment { EnvironmentName = options.Environment })。
        // 用 TryAddSingleton: 宿主若先放了自己的实现(知道真实环境), 这里不覆盖它。
        services.TryAddSingleton<ISnowHostEnvironment>(
            new SnowHostEnvironment { EnvironmentName = options.Environment });

        IReadOnlyList<SnowModuleDescriptor> modules = ModuleLoader.LoadModules(services, startupModuleType);

        ModuleContainer container = new(modules);
        SnowApplication application = new(services, container);

        services.AddSingleton<IModuleContainer>(container);
        services.AddSingleton(application);

        await application.ConfigureServicesAsync();

        return application;
    }
}
