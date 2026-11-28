namespace Snow.AspNetCore.Authentication;

/// <summary>
/// 会话: 一条记录对应一个仍然有效的令牌
/// <para>白名单模型下它就是令牌有效性的凭据 —— 会话被删(登出/踢人/封禁)或缓存被清, 令牌立刻失效。</para>
/// <para>到期时间由服务端滑动续期: 每次访问都往后推一个窗口。</para>
/// </summary>
public sealed class TokenSession
{
    /// <summary>
    /// 令牌唯一标识(令牌里的 <c>jti</c>), 会话按它存储
    /// </summary>
    public string Jti { get; set; } = string.Empty;

    /// <summary>
    /// 登录Id(业务用户Id 的字符串形式)
    /// </summary>
    public string LoginId { get; set; } = string.Empty;

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 当前到期时间: 初始为登录时间加一个窗口, 之后每次访问都往后推一个窗口
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 登录来源IP, 未记录时为 null
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 登录来源设备标识(通常是 User-Agent), 未记录时为 null
    /// </summary>
    public string? UserAgent { get; set; }
}
