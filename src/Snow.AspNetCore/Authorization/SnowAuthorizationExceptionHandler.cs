using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Snow.Authorization;
using Snow.DependencyInjection;

namespace Snow.AspNetCore.Authorization;

/// <summary>
/// 默认授权异常处理器(对应 ABP 的 <c>AbpAuthorizationExceptionHandler</c>):
/// 已认证主体被禁止(403), 未认证主体走质询(通常 401)。
/// </summary>
[TransientService]
public class SnowAuthorizationExceptionHandler : ISnowAuthorizationExceptionHandler
{
    /// <inheritdoc />
    public virtual async Task HandleAsync(SnowAuthorizationException exception, HttpContext httpContext)
    {
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            await httpContext.ForbidAsync();
        }
        else
        {
            await httpContext.ChallengeAsync();
        }
    }
}
