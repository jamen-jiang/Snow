using Microsoft.Extensions.DependencyInjection;
using Snow.Localization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 本地化的服务登记入口
/// </summary>
public static class SnowLocalizationServiceCollectionExtensions
{
    /// <summary>
    /// 追加本地化资源
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="resourceName">资源名</param>
    /// <param name="configure">资源内容配置动作</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowLocalizationResource(
        this IServiceCollection services,
        string resourceName,
        Action<LocalizationResource>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        services.Configure<SnowLocalizationOptions>(options =>
        {
            if (!options.Resources.TryGetValue(resourceName, out LocalizationResource? resource))
            {
                resource = new LocalizationResource();
                options.Resources[resourceName] = resource;
            }

            configure?.Invoke(resource);
        });

        return services;
    }
}
