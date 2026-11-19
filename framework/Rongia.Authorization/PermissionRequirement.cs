using Microsoft.AspNetCore.Authorization;

namespace Rongia.Authorization;

/// <summary>
/// 权限授权要求: 承载一个权限名
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionName">权限名</param>
    public PermissionRequirement(string permissionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);
        PermissionName = permissionName;
    }

    /// <summary>
    /// 权限名
    /// </summary>
    public string PermissionName { get; }
}
