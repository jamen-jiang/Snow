using Microsoft.AspNetCore.Builder;

namespace Snow.AspNetCore;

/// <summary>
/// 中间件幂等挂载
/// <para>包自己在应用初始化阶段挂中间件之后, 宿主沿用手写的 <c>UseSnowXxx()</c> 也不会重复执行:
/// 挂载过的中间件类型记在应用构建器的 <c>Properties</c> 里, 第二次调用直接返回。</para>
/// </summary>
public static class SnowMiddlewareApplicationBuilderExtensions
{
    private const string KeyPrefix = "Snow.Middleware.";

    /// <summary>
    /// 挂载中间件; 同一个中间件类型在一次应用构建里只挂一次
    /// </summary>
    /// <typeparam name="TMiddleware">中间件类型</typeparam>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowMiddleware<TMiddleware>(this IApplicationBuilder app)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.IsSnowMiddlewareMounted(typeof(TMiddleware)))
        {
            return app;
        }

        app.MarkSnowMiddlewareMounted(typeof(TMiddleware));
        return app.UseMiddleware<TMiddleware>();
    }

    /// <summary>
    /// 判断中间件是否已经挂过
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="middlewareType">中间件类型</param>
    /// <returns>已挂过返回 true</returns>
    public static bool IsSnowMiddlewareMounted(this IApplicationBuilder app, Type middlewareType)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(middlewareType);
        return app.Properties.ContainsKey(KeyPrefix + middlewareType.FullName);
    }

    /// <summary>
    /// 标记中间件已挂载
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="middlewareType">中间件类型</param>
    public static void MarkSnowMiddlewareMounted(this IApplicationBuilder app, Type middlewareType)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(middlewareType);
        app.Properties[KeyPrefix + middlewareType.FullName] = true;
    }
}
