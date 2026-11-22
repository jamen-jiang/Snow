using Microsoft.Extensions.DependencyInjection;

namespace Snow.Json;

/// <summary>
/// JSON 的服务登记入口
/// </summary>
public static class SnowJsonServiceCollectionExtensions
{
    /// <summary>
    /// 调整全局 JSON 约定
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowJson(this IServiceCollection services, Action<SnowJsonOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services;
    }
}
