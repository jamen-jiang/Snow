using Microsoft.AspNetCore.Authorization;

namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态鉴权要求: 一个类型覆盖 登录/权限/角色 三种校验
/// </summary>
public sealed class SaTokenRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="kind">校验类型</param>
    /// <param name="values">权限标识或角色名, 登录校验时为空</param>
    public SaTokenRequirement(SaTokenCheckKind kind, params string[] values)
    {
        Kind = kind;
        Values = values;
    }

    /// <summary>
    /// 校验类型
    /// </summary>
    public SaTokenCheckKind Kind { get; }

    /// <summary>
    /// 权限标识或角色名
    /// </summary>
    public string[] Values { get; }
}
