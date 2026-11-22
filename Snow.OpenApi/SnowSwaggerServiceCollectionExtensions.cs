using Microsoft.Extensions.DependencyInjection;

namespace Snow.OpenApi;

/// <summary>
/// OpenAPI 的服务登记入口
/// </summary>
public static class SnowSwaggerServiceCollectionExtensions
{
    /// <summary>
    /// 在模块的 PreConfigureServices 阶段调整 OpenAPI 选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowSwagger(this IServiceCollection services, Action<SnowSwaggerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        SnowSwaggerOptionsSetup? setup = services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowSwaggerOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowSwaggerOptionsSetup>()
            .FirstOrDefault();

        if (setup is null)
        {
            setup = new SnowSwaggerOptionsSetup();
            services.AddSingleton(setup);
        }

        setup.Add(configure);
        return services;
    }
}
