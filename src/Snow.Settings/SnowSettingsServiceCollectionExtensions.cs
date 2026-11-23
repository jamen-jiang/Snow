using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Snow.Settings;

/// <summary>
/// 设置的服务登记入口
/// </summary>
public static class SnowSettingsServiceCollectionExtensions
{
    /// <summary>
    /// 换掉设置存储实现(默认是 <see cref="NullSettingStore"/>)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <typeparam name="TStore">存储实现类型</typeparam>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowSettingStore<TStore>(this IServiceCollection services)
        where TStore : class, ISettingStore
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Singleton<ISettingStore, TStore>());
        return services;
    }
}
