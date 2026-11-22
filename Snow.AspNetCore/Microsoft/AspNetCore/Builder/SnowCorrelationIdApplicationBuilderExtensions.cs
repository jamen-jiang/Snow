using Snow.AspNetCore;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 链路标识中间件挂载
/// </summary>
public static class SnowCorrelationIdApplicationBuilderExtensions
{
    private const string Marker = "Snow.Middleware.CorrelationId";

    /// <summary>
    /// 挂上链路标识: 必须在所有可能短路的地方之外 —— 认证失败、授权拒绝、路由没匹配时响应也要能带回 X-Correlation-Id
    /// <para>重复调用不会重复挂。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Properties.ContainsKey(Marker))
        {
            return app;
        }

        app.Properties[Marker] = true;
        return app.UseMiddleware<SnowCorrelationIdMiddleware>();
    }
}
