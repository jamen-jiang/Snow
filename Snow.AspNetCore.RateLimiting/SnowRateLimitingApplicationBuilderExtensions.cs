using Microsoft.AspNetCore.Builder;

namespace Snow.AspNetCore.RateLimiting;

/// <summary>
/// 限流中间件挂载
/// </summary>
public static class SnowRateLimitingApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上微软自带的限流中间件, 位置在 UseRouting 之后、端点之前
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseRateLimiter();
    }
}
