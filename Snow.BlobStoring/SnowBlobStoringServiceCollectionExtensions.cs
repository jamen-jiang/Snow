using Microsoft.Extensions.DependencyInjection;

namespace Snow.BlobStoring;

/// <summary>
/// 对象存储的服务登记入口
/// </summary>
public static class SnowBlobStoringServiceCollectionExtensions
{
    /// <summary>
    /// 换掉存储后端
    /// <para>只在 <c>AddApplication</c> 之前调用有效: 本模块的 ConfigureServices 会先应用这些动作再登记 IBlobProvider。</para>
    /// </summary>
    /// <typeparam name="TProvider">后端实现类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowBlobStoring<TProvider>(this IServiceCollection services)
        where TProvider : class, IBlobProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSnowBlobStoring(options => options.ProviderType = typeof(TProvider));
    }

    /// <summary>
    /// 调整对象存储选项
    /// <para>只在 <c>AddApplication</c> 之前调用有效: 本模块的 ConfigureServices 会先应用这些动作再登记 IBlobProvider。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowBlobStoring(this IServiceCollection services, Action<SnowBlobStoringOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        SnowBlobStoringOptionsSetup? setup = services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowBlobStoringOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowBlobStoringOptionsSetup>()
            .FirstOrDefault();

        if (setup is null)
        {
            setup = new SnowBlobStoringOptionsSetup();
            services.AddSingleton(setup);
        }

        setup.Add(configure);
        return services;
    }
}
