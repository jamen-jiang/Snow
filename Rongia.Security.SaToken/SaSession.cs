namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态: 登录时创建, 之后每次请求按令牌查回(对应 Sa-Token 的 SaSession)
/// </summary>
public sealed class SaSession
{
    /// <summary>
    /// 令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 登录Id(业务用户Id 的字符串形式)
    /// </summary>
    public string LoginId { get; set; } = string.Empty;

    /// <summary>
    /// 登录时写入的声明, 每次请求还原成 ClaimsPrincipal
    /// </summary>
    public List<SaClaim> Claims { get; set; } = [];

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 最后活跃时间
    /// </summary>
    public DateTimeOffset LastActiveTime { get; set; }

    /// <summary>
    /// 绝对过期时间
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
