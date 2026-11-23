using Microsoft.AspNetCore.Authorization;

namespace Snow.Authorization;

/// <summary>
/// "任一权限"授权要求: 承载多个权限名, 拥有其中任意一个即通过
/// <para>由 <see cref="SnowAuthorizationPolicyProvider"/> 在策略名里用逗号写了多个权限时构造,
/// 也就是 <c>[Authorize("Snow.Orders.Create,Snow.Orders.Update")]</c> 的实际含义 —— 满足其一即可。</para>
/// </summary>
public sealed class AnyPermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionNames">权限名, 至少一个</param>
    public AnyPermissionRequirement(IReadOnlyList<string> permissionNames)
    {
        ArgumentNullException.ThrowIfNull(permissionNames);
        if (permissionNames.Count == 0)
        {
            throw new ArgumentException("至少需要一个权限名。", nameof(permissionNames));
        }

        foreach (string permissionName in permissionNames)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);
        }

        PermissionNames = permissionNames;
    }

    /// <summary>
    /// 权限名列表
    /// </summary>
    public IReadOnlyList<string> PermissionNames { get; }
}
