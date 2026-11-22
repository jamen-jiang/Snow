using Microsoft.Extensions.DependencyInjection;

namespace Snow.AspNetCore.SignalR;

/// <summary>
/// SignalR 的服务登记入口
/// </summary>
public static class SnowSignalRServiceCollectionExtensions
{
    /// <summary>
    /// 显式登记一个 Hub。必须在 <c>AddApplication</c> 之前调用
    /// </summary>
    /// <typeparam name="THub">Hub 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="path">端点路径, 省略则按类名推导</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowHub<THub>(this IServiceCollection services, string? path = null)
        where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        ArgumentNullException.ThrowIfNull(services);

        GetOrCreateRegistry(services).Add(typeof(THub), path ?? SnowHubPath.For(typeof(THub)));
        return services;
    }

    /// <summary>
    /// 调整 SignalR 选项。必须在 <c>AddApplication</c> 之前调用
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowSignalR(this IServiceCollection services, Action<SnowSignalROptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        GetOrCreateRegistry(services);
        return services;
    }

    internal static SnowHubRegistry GetOrCreateRegistry(IServiceCollection services)
    {
        SnowHubRegistry? registry = services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowHubRegistry))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowHubRegistry>()
            .FirstOrDefault();

        if (registry is null)
        {
            registry = new SnowHubRegistry();
            services.AddSingleton(registry);
        }

        return registry;
    }
}
