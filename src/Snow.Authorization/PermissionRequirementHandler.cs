using Microsoft.AspNetCore.Authorization;
using Snow.Authorization.Permissions;
using Snow.DependencyInjection;

namespace Snow.Authorization;

/// <summary>
/// 权限授权处理器: 调用 <see cref="IPermissionChecker"/> 判定是否放行
/// <para>只要求已认证 + 拥有权限; 未认证直接不通过(由认证中间件先给出 401)。</para>
/// </summary>
[ScopedService]
[ExposeServices(typeof(IAuthorizationHandler))]
public sealed class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionChecker _permissionChecker;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionChecker">权限校验器</param>
    public PermissionRequirementHandler(IPermissionChecker permissionChecker)
    {
        ArgumentNullException.ThrowIfNull(permissionChecker);

        _permissionChecker = permissionChecker;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (await _permissionChecker.IsGrantedAsync(context.User, requirement.PermissionName))
        {
            context.Succeed(requirement);
        }
    }
}
