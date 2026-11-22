using Microsoft.AspNetCore.Authorization;

namespace Snow.Authentication;

/// <summary>
/// 注解鉴权类型
/// </summary>
public enum TokenCheckKind
{
    /// <summary>
    /// 仅校验登录
    /// </summary>
    Login = 0,

    /// <summary>
    /// 校验角色
    /// </summary>
    Role = 1,
}

/// <summary>
/// 令牌鉴权要求: 一个类型覆盖 登录/角色 两种校验
/// <para>权限校验不在这里: 那是 <c>Snow.Authorization</c> 的 <c>[RequirePermission]</c> 的职责。</para>
/// </summary>
public sealed class TokenRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="kind">校验类型</param>
    /// <param name="values">角色名, 登录校验时为空</param>
    public TokenRequirement(TokenCheckKind kind, params string[] values)
    {
        Kind = kind;
        Values = values;
    }

    /// <summary>
    /// 校验类型
    /// </summary>
    public TokenCheckKind Kind { get; }

    /// <summary>
    /// 角色名
    /// </summary>
    public string[] Values { get; }
}

/// <summary>
/// 校验登录: 标在控制器或动作上, 未登录返回 401
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class TokenCheckLoginAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    public TokenCheckLoginAttribute() => Policy = TokenPolicyProvider.LoginPolicy;
}

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
