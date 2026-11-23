using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
