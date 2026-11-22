using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Snow.Authentication;

/// <summary>
/// 把登录态异常翻译成 HTTP 状态码: 未登录 401, 缺角色/被封禁 403
/// <para>MVC 宿主由模块自动注册的 <see cref="TokenExceptionFilter"/> 接管; 最小 API 与其它管线用
/// <c>app.UseSnowTokenExceptionHandling()</c>。不接的话 <see cref="TokenUtil.GetLoginId"/> 抛出的异常会变成 500。</para>
/// </summary>
public static class TokenExceptionHandling
{
    /// <summary>
    /// 尝试把异常写成响应
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <param name="exception">抛出的异常</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否已处理; false 表示不是登录态异常, 交给外层继续抛</returns>
    public static async Task<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);

        if (!TryMap(exception, out int statusCode, out string title))
        {
            return false;
        }

        if (context.Response.HasStarted)
        {
            // 响应已经开始写, 改不了状态码了
            return true;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        ProblemDetails problem = new()
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
        };

        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }

    private static bool TryMap(Exception exception, out int statusCode, out string title)
    {
        switch (exception)
        {
            case TokenNotLoginException:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "未登录";
                return true;

            case TokenNotRoleException:
                statusCode = StatusCodes.Status403Forbidden;
                title = "缺少角色";
                return true;

            case TokenDisabledException:
                statusCode = StatusCodes.Status403Forbidden;
                title = "账号已被封禁";
                return true;

            default:
                statusCode = 0;
                title = string.Empty;
                return false;
        }
    }
}

/// <summary>
/// MVC 异常过滤器: 登录态异常转 401/403, 其它异常原样交给后续过滤器
/// </summary>
public sealed class TokenExceptionFilter : IAsyncExceptionFilter
{
    /// <inheritdoc />
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await TokenExceptionHandling.TryHandleAsync(context.HttpContext, context.Exception, context.HttpContext.RequestAborted))
        {
            context.ExceptionHandled = true;
        }
    }
}

/// <summary>
/// 最小 API 用的异常中间件: 包在管线最外层, 只拦登录态异常
/// </summary>
public sealed class TokenExceptionMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="next">下一个中间件</param>
    public TokenExceptionMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="context">当前请求</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (exception is TokenException)
        {
            if (!await TokenExceptionHandling.TryHandleAsync(context, exception, context.RequestAborted))
            {
                throw;
            }
        }
    }
}

/// <summary>
/// 注册辅助
/// </summary>
public static class TokenExceptionHandlingExtensions
{
    /// <summary>
    /// 挂上登录态异常中间件(最小 API 宿主用; MVC 宿主已被 <see cref="TokenExceptionFilter"/> 覆盖, 重复挂也无害)
    /// </summary>
    /// <param name="app">应用构建器</param>
    public static IApplicationBuilder UseSnowTokenExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<TokenExceptionMiddleware>();
    }
}
