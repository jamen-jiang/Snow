using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.AspNetCore.RateLimiting;

/// <summary>
/// 限流的服务登记入口
/// </summary>
public static class SnowRateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// 追加限流配置。必须在 <c>AddApplication</c> 之前调用, 否则本模块的 ConfigureServices 已经跑过, 动作不会生效
    /// <para>参数是微软自带的 <see cref="RateLimiterOptions"/>, 直接写 GlobalLimiter 与 AddFixedWindowLimiter 等方法。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">限流配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowRateLimiting(this IServiceCollection services, Action<RateLimiterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        SnowRateLimitingOptionsSetup? setup = services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowRateLimitingOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowRateLimitingOptionsSetup>()
            .FirstOrDefault();

        if (setup is null)
        {
            setup = new SnowRateLimitingOptionsSetup();
            services.AddSingleton(setup);
        }

        setup.Add(configure);
        return services;
    }
}
