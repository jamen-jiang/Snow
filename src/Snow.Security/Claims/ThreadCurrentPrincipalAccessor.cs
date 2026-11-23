using System.Security.Claims;
using Snow.DependencyInjection;

namespace Snow.Security.Claims;

/// <summary>
/// 基于 <see cref="Thread.CurrentPrincipal"/> 的主体访问器: 非 Web 宿主(后台任务/控制台)的兜底实现
/// </summary>
[SingletonService]
public class ThreadCurrentPrincipalAccessor : CurrentPrincipalAccessorBase
{
    /// <inheritdoc />
    protected override ClaimsPrincipal GetClaimsPrincipal()
    {
        System.Security.Principal.IPrincipal? principal = Thread.CurrentPrincipal;
        if (principal == null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        return principal as ClaimsPrincipal ?? new ClaimsPrincipal(principal);
    }
}
