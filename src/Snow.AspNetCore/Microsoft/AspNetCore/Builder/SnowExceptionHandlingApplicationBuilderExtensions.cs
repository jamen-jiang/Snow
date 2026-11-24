using Microsoft.AspNetCore.Builder;
using Snow.AspNetCore.ExceptionHandling;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 异常处理的管道入口
/// </summary>
public static class SnowExceptionHandlingApplicationBuilderExtensions
{
    /// <summary>
    /// 挂上异常处理中间件(对应 ABP 的 <c>UseAbpExceptionHandling()</c>)
    /// <para>放在管道最前面, 才能兜住认证、授权、端点抛出的所有异常; 依赖 <c>AddSnowExceptionHandling()</c> 的登记。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<SnowExceptionHandlingMiddleware>();
    }
}
