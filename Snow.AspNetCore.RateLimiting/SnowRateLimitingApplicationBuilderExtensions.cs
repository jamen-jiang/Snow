using Microsoft.AspNetCore.Builder;

namespace Snow.AspNetCore.RateLimiting;

/// <summary>
/// 限流中间件挂载
/// </summary>
public static class SnowRateLimitingApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上微软自带的限流中间件, 位置在 UseRouting 之后、端点之前
    /// <para>正常情况不用调: <c>SnowRateLimitingModule</c> 已经把它登记进管道清单自动挂好; 保留此方法给单独使用的宿主, 重复调用不会重复挂。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseRateLimiter();
    }
}