using Microsoft.AspNetCore.Builder;

namespace Snow.AspNetCore;

/// <summary>
/// 链路标识中间件注册扩展(对应 ABP 的 <c>UseCorrelationId()</c>)
/// </summary>
public static class SnowCorrelationIdApplicationBuilderExtensions
{
    /// <summary>
    /// 启用链路标识中间件, 应尽早放在管道里(在需要日志增强/审计的中间件之前)
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器</returns>
    public static IApplicationBuilder UseSnowCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseSnowMiddlewareOnce<SnowCorrelationIdMiddleware>();
    }
}