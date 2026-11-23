using Microsoft.AspNetCore.Authorization;

namespace Snow.Authorization;

/// <summary>
/// 权限授权要求: 承载一个权限名, 由 <see cref="PermissionRequirementHandler"/> 判定
/// <para>由 <see cref="SnowAuthorizationPolicyProvider"/> 在策略名命中权限定义时构造, 一般不用手写。</para>
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
