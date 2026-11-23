using Snow.Modularity;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 限流中间件挂载
/// </summary>
public static class SnowRateLimitingApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上限流: 必须在 <c>UseRouting()</c> 之后, 端点元数据上的限流策略才读得到
    /// <para>微软的 <c>RateLimitingMiddleware</c> 是 internal(拿不到类型), 所以这里用公开的 <c>UseRateLimiter()</c>。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseRateLimiter();
    }
}
