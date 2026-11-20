using Microsoft.AspNetCore.Authorization;
using Snow.Authorization.Permissions;

namespace Snow.Security.SaToken;

/// <summary>
/// 登录态鉴权处理器: 登录看是否已认证, 角色看角色声明, 权限交给 <see cref="IPermissionChecker"/>(支持运行期授予)
/// </summary>
public sealed class SaTokenRequirementHandler : AuthorizationHandler<SaTokenRequirement>
{
    private readonly IPermissionChecker _permissionChecker;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionChecker">权限校验器</param>
    public SaTokenRequirementHandler(IPermissionChecker permissionChecker)
    {
        ArgumentNullException.ThrowIfNull(permissionChecker);
        _permissionChecker = permissionChecker;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SaTokenRequirement requirement)
    {
        switch (requirement.Kind)
        {
            case SaTokenCheckKind.Login:
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    context.Succeed(requirement);
                }

                break;

            case SaTokenCheckKind.Role:
                if (requirement.Values.Any(role => context.User.IsInRole(role)))
                {
                    context.Succeed(requirement);
                }

                break;

            case SaTokenCheckKind.Permission:
                if (requirement.Values.Length == 0)
                {
                    break;
                }

                foreach (string permission in requirement.Values)
                {
                    if (!await _permissionChecker.IsGrantedAsync(context.User, permission))
                    {
                        return;
                    }
                }

                context.Succeed(requirement);
                break;
        }
    }
}
