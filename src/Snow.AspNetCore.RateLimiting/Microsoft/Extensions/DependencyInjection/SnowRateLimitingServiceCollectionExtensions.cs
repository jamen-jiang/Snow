using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Snow.AspNetCore.RateLimiting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 限流的服务登记入口
/// </summary>
public static class SnowRateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// 追加限流配置。建议在 <c>AddApplication</c> 之前调用: 走标准 Options 管道(PostConfigure), 启动时结算, 后登记者覆盖先登记者
    /// <para>参数是微软自带的 <see cref="RateLimiterOptions"/>, 直接写 GlobalLimiter 与 AddFixedWindowLimiter 等方法。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">限流配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowRateLimiting(this IServiceCollection services, Action<RateLimiterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.PostConfigure(configure);
        return services;
    }
}
