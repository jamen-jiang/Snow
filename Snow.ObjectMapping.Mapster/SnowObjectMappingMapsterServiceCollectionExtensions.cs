using Microsoft.Extensions.DependencyInjection;

namespace Snow.ObjectMapping.Mapster;

/// <summary>
/// Mapster 映射的服务登记入口
/// </summary>
public static class SnowObjectMappingMapsterServiceCollectionExtensions
{
    /// <summary>
    /// 在模块的 PreConfigureServices 阶段调整 Mapster 映射选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowObjectMapping(this IServiceCollection services, Action<SnowObjectMappingMapsterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        SnowObjectMappingMapsterOptionsSetup? setup = services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowObjectMappingMapsterOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowObjectMappingMapsterOptionsSetup>()
            .FirstOrDefault();

        if (setup is null)
        {
            setup = new SnowObjectMappingMapsterOptionsSetup();
            services.AddSingleton(setup);
        }

        setup.Add(configure);
        return services;
    }
}
