using Microsoft.Extensions.DependencyInjection;

namespace Snow.Json;

/// <summary>
/// System.Text.Json 实现的选项登记入口
/// </summary>
public static class SnowSystemTextJsonServiceCollectionExtensions
{
    /// <summary>
    /// 调整 System.Text.Json 的库特有选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowSystemTextJson(this IServiceCollection services, Action<SnowSystemTextJsonOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services;
    }
}
