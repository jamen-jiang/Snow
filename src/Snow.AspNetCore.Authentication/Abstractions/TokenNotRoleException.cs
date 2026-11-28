using Snow.Exceptions;

namespace Snow.AspNetCore.Authentication;

/// <summary>
/// 缺少角色异常: 由全局异常中间件按 403 回给调用方
/// </summary>
public sealed class TokenNotRoleException : TokenException, IHasHttpStatusCode
{
    /// <summary>
    /// 构造器
    /// </summary>
    public TokenNotRoleException(params string[] roles)
        : base($"缺少角色: {string.Join(", ", roles)}") => Roles = roles;

    /// <summary>
    /// 缺失的角色
    /// </summary>
    public string[] Roles { get; }

    /// <inheritdoc />
    public int HttpStatusCode => 403;
}
