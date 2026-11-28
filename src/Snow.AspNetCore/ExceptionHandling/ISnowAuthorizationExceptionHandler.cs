using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Snow.Authorization;

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 授权异常处理器(对应 ABP 的 <c>IAbpAuthorizationExceptionHandler</c>): 决定未认证/无权限时如何响应(质询或禁止)。
/// <para>放在 Web 包而不是 Snow.Authorization: 参数要 HTTP 上下文, 与 ABP 把它放在 <c>Volo.Abp.AspNetCore</c> 同理。</para>
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
