using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Snow.DependencyInjection;

/// <summary>
/// 服务集合的通用扩展(对应 ABP 的 ServiceCollectionCommonExtensions + ServiceCollectionConfigurationExtensions)
/// </summary>
public static class ServiceCollectionCommonExtensions
{
    /// <summary>
    /// 用宿主手上的配置对象顶掉服务集合里已有的 IConfiguration
    /// <para>对应 ABP 的 <c>ReplaceConfiguration()</c>: <c>WebApplicationBuilderExtensions.AddApplicationAsync()</c>
    /// 靠它把 <c>builder.Configuration</c> 交给模块, 宿主因此不必再手写
    /// <c>services.AddSingleton&lt;IConfiguration&gt;(builder.Configuration)</c>。</para>
    /// <para>用 Replace 而不是 Add: 前者会先摘掉同类型的已有登记, 保证模块取到的是这一份。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">宿主配置对象</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection ReplaceConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.Replace(ServiceDescriptor.Singleton<IConfiguration>(configuration));
    }

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
