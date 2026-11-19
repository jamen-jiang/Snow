namespace Rongia.Security.SaToken;

/// <summary>
/// 令牌信息
/// </summary>
public sealed class SaTokenInfo
{
    /// <summary>
    /// 令牌参数名
    /// </summary>
    public string TokenName { get; init; } = string.Empty;

    /// <summary>
    /// 令牌值
    /// </summary>
    public string TokenValue { get; init; } = string.Empty;

    /// <summary>
    /// 登录Id
    /// </summary>
    public long LoginId { get; init; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; init; }

    /// <summary>
    /// 绝对过期时间
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}
