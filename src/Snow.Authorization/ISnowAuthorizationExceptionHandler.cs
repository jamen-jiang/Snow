using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Snow.Authorization;

/// <summary>
/// 授权异常处理器(对应 ABP 的 <c>IAbpAuthorizationExceptionHandler</c>): 决定未认证/无权限时如何响应(质询或禁止)。
/// </summary>
public interface ISnowAuthorizationExceptionHandler
{
    /// <summary>
    /// 处理授权异常
    /// </summary>
    /// <param name="exception">授权异常</param>
    /// <param name="httpContext">当前 HTTP 上下文</param>
    Task HandleAsync(SnowAuthorizationException exception, HttpContext httpContext);
}
