namespace Rongia.Security.SaToken;

/// <summary>
/// 缺少角色异常
/// </summary>
public sealed class SaNotRoleException : SaTokenException
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="roles">缺失的角色</param>
    public SaNotRoleException(params string[] roles)
        : base($"缺少角色: {string.Join(", ", roles)}") => Roles = roles;

    /// <summary>
    /// 缺失的角色
    /// </summary>
    public string[] Roles { get; }
}
