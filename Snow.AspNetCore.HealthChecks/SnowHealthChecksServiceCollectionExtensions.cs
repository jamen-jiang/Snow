using Microsoft.Extensions.DependencyInjection;

namespace Snow.AspNetCore.HealthChecks;

/// <summary>
/// 健康检查的服务登记入口
/// </summary>
public static class SnowHealthChecksServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个健康检查项。必须在 <c>AddApplication</c> 之前调用
    /// </summary>
    /// <typeparam name="THealthCheck">实现 IHealthCheck 的类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="name">检查项名称</param>
    /// <param name="tags">标签</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowHealthCheck<THealthCheck>(this IServiceCollection services, string name, params string[] tags)
        where THealthCheck : class, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        ArgumentNullException.ThrowIfNull(services);

        GetOrCreateRegistry(services).Add(name, typeof(THealthCheck), tags);
        return services;
    }

    /// <summary>
    /// 调整健康检查端点选项。必须在 <c>AddApplication</c> 之前调用
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowHealthChecks(this IServiceCollection services, Action<SnowHealthChecksOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        GetOrCreateRegistry(services);
        return services;
    }

    internal static SnowHealthCheckRegistry GetOrCreateRegistry(IServiceCollection services)
    {
        SnowHealthCheckRegistry? registry = services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowHealthCheckRegistry))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowHealthCheckRegistry>()
            .FirstOrDefault();

        if (registry is null)
        {
            registry = new SnowHealthCheckRegistry();
            services.AddSingleton(registry);
        }

        return registry;
    }
}
