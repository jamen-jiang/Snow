namespace Snow.Authentication;

/// <summary>
/// 令牌信息
/// </summary>
public sealed class TokenInfo
{
    /// <summary>
    /// 令牌所在的请求头, 固定 "Authorization"
    /// </summary>
    public string TokenName { get; init; } = TokenResolver.HeaderName;

    /// <summary>
    /// 令牌类型, 固定 "Bearer"(RFC 6750)
    /// </summary>
    public string TokenType { get; init; } = TokenResolver.Scheme;

    /// <summary>
    /// 令牌值(完整 JWT)
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// 令牌唯一标识
    /// </summary>
    public string Jti { get; init; } = string.Empty;

    /// <summary>
    /// 登录Id: 与 <see cref="TokenSession.LoginId"/> 一致, 是用户Id 的字符串形式(不假定一定是数字)
    /// </summary>
    public string LoginId { get; init; } = string.Empty;

    /// <summary>
    /// 签发时间
    /// </summary>
    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// 当前到期时间: 取会话的到期时间, 滑动续期后会晚于令牌 <c>exp</c> 里写的初始值
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}
