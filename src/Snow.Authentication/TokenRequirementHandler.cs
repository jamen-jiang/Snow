using Microsoft.AspNetCore.Authorization;
using Snow.DependencyInjection;

namespace Snow.Authentication;

/// <summary>
/// 令牌鉴权处理器: 登录看是否已认证, 角色看角色声明
/// <para>权限校验由 <c>Snow.Authorization</c> 的 <c>PermissionRequirementHandler</c> 负责, 这里不再重复实现。</para>
/// </summary>
[ScopedService]
[ExposeServices(typeof(IAuthorizationHandler))]
public sealed class TokenRequirementHandler : AuthorizationHandler<TokenRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TokenRequirement requirement)
    {
        switch (requirement.Kind)
        {
            case TokenCheckKind.Login:
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    context.Succeed(requirement);
                }

                break;

            case TokenCheckKind.Role:
                if (requirement.Values.Any(role => context.User.IsInRole(role)))
                {
                    context.Succeed(requirement);
                }

                break;
        }

        return Task.CompletedTask;
    }
}
