using Snow.AspNetCore;
using Snow.AspNetCore.Serilog;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 请求管道扩展
/// </summary>
public static class SnowAspNetCoreSerilogApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上请求级日志增强中间件(应尽早调用, 让后续中间件与业务日志都带上用户字段)
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器</returns>
    public static IApplicationBuilder UseSnowSerilogEnrichers(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseSnowMiddleware<SnowSerilogMiddleware>();
    }
}