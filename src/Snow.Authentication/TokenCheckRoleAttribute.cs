using Microsoft.AspNetCore.Authorization;

namespace Snow.Authentication;

/// <summary>
/// 校验角色(具备任一角色即通过): 缺失返回 403
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class TokenCheckRoleAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="roles">角色名</param>
    public TokenCheckRoleAttribute(params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        RoleNames = roles;
        Policy = TokenPolicyProvider.RolePolicyPrefix + string.Join(',', roles);
    }

    /// <summary>
    /// 角色名
    /// </summary>
    public string[] RoleNames { get; }
}
