using Microsoft.Extensions.DependencyInjection;

namespace Snow.Json.Newtonsoft;

/// <summary>
/// Newtonsoft.Json 实现的服务登记入口
/// </summary>
public static class SnowNewtonsoftJsonServiceCollectionExtensions
{
    /// <summary>
    /// 调整 Newtonsoft.Json 的库特有选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowNewtonsoftJson(this IServiceCollection services, Action<SnowNewtonsoftJsonOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services;
    }
}
