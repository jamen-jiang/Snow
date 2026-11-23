using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Snow.AspNetCore;

namespace Snow.Authentication;

/// <summary>
/// 把登录态异常翻译成 HTTP 状态码: 未登录 401, 缺角色/被封禁 403
/// <para>MVC 宿主由模块自动注册的 <see cref="TokenExceptionFilter"/> 接管; 最小 API 与其它管线由模块登记的管道贡献者接管,
/// 它挂在 <c>OnBeforeRouting</c>(路由之前), 包住认证阶段。不接的话 <see cref="TokenUtil.GetLoginId"/> 抛出的异常会变成 500。</para>
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
/// 最小 API 用的异常中间件: 只拦登录态异常
/// <para>由认证模块登记成管道贡献者, 挂在 <c>OnBeforeRouting</c>(路由之前、认证之前):
/// 兜住它内层所有中间件(含认证阶段 <c>JwtBearer</c>)与端点抛的登录态异常。
/// 链路标识在骨架最外层, 所以异常在这里被捕获时链路标识的作用域还活着, 错误响应仍能带回 X-Correlation-Id。</para>
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
    /// 挂上登录态异常中间件
    /// <para>由 <c>SnowAuthenticationModule</c> 登记成管道贡献者, 挂在 <c>OnBeforeRouting</c> —— 位置在 <c>UseAuthentication()</c> 之前,
    /// 这样认证阶段抛的令牌异常也在它内层。MVC 宿主另有 <see cref="TokenExceptionFilter"/> 覆盖控制器内的同一批异常。</para>
    /// <para>挂在 <c>UseAuthentication()</c> 之后只覆盖控制器与端点里的异常, <c>JwtBearer</c> 在认证阶段抛的令牌异常会漏成 500。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>同一个应用构建器, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowTokenExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<TokenExceptionMiddleware>();
    }
}
