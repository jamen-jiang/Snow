using Microsoft.AspNetCore.Authorization;

namespace Snow.Authentication;

/// <summary>
/// 令牌鉴权要求: 一个类型覆盖 登录/角色 两种校验
/// <para>权限校验不在这里: 那是 <c>Snow.Authorization</c> 的职责, 注解直接写 <c>[Authorize("权限名")]</c>。</para>
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
