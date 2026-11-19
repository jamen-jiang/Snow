using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.DependencyInjection;

/// <summary>
/// 服务集合的通用扩展(对应 ABP 的 ServiceCollectionCommonExtensions)
/// </summary>
public static class ServiceCollectionCommonExtensions
{
    /// <summary>
    /// 取宿主已注册的配置对象,模块里的 ServiceConfigurationContext.Configuration 即由它提供
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>配置对象</returns>
    /// <exception cref="InvalidOperationException">服务集合中没有以实例方式注册的 IConfiguration</exception>
    public static IConfiguration GetConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.GetSingletonInstanceOrNull<IConfiguration>()
            ?? throw new InvalidOperationException(
                "服务集合中没有以实例方式注册的 IConfiguration,宿主需在 AddApplication 之前执行 "
                + "services.AddSingleton<IConfiguration>(configuration)");
    }
}
