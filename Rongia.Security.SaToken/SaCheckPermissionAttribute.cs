using Microsoft.AspNetCore.Authorization;

namespace Rongia.Security.SaToken;

/// <summary>
/// 校验权限(多个权限需全部具备): 缺失返回 403
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class SaCheckPermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissions">权限标识, 如 order:add</param>
    public SaCheckPermissionAttribute(params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        Permissions = permissions;
        Policy = SaTokenPolicyProvider.PermissionPolicyPrefix + string.Join(',', permissions);
    }

    /// <summary>
    /// 权限标识
    /// </summary>
    public string[] Permissions { get; }
}
