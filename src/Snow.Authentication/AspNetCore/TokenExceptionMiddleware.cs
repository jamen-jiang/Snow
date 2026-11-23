using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Snow.Authentication;

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
