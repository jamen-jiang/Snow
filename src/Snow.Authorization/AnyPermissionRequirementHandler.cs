using Microsoft.AspNetCore.Authorization;
using Snow.Authorization.Permissions;
using Snow.DependencyInjection;

namespace Snow.Authorization;

/// <summary>
/// "任一权限"授权处理器: 逐个问 <see cref="IPermissionChecker"/>, 命中一个就放行
/// <para>检查顺序与写法一致, 命中即停; 全部不命中才不通过。</para>
/// </summary>
[ScopedService]
[ExposeServices(typeof(IAuthorizationHandler))]
public sealed class AnyPermissionRequirementHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly IPermissionChecker _permissionChecker;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionChecker">权限校验器</param>
    public AnyPermissionRequirementHandler(IPermissionChecker permissionChecker)
    {
        ArgumentNullException.ThrowIfNull(permissionChecker);

        _permissionChecker = permissionChecker;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        foreach (string permissionName in requirement.PermissionNames)
        {
            if (await _permissionChecker.IsGrantedAsync(context.User, permissionName))
            {
                context.Succeed(requirement);

                return;
            }
        }
    }
}
