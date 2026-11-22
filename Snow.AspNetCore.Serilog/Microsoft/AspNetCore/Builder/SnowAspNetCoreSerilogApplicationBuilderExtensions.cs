using Snow.AspNetCore.Serilog;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 请求级日志增强挂载(对应 ABP 的 <c>AbpSerilogApplicationBuilderExtensions</c>)
/// </summary>
public static class SnowAspNetCoreSerilogApplicationBuilderExtensions
{
    private const string Marker = "Snow.Middleware.Serilog";

    /// <summary>
    /// 挂上请求级日志增强中间件: 应尽早调用, 让后续中间件与业务日志都带上用户字段
    /// <para>重复调用不会重复挂。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowSerilogEnrichers(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Properties.ContainsKey(Marker))
        {
            return app;
        }

        app.Properties[Marker] = true;
        return app.UseMiddleware<SnowSerilogMiddleware>();
    }
}
