namespace Snow.Authentication;

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

/// <summary>
/// 登录来源信息: 写进会话, 让"在线设备列表"能显示是哪台机器
/// <para>只作展示与人工挑选下线目标, 不参与任何校验。</para>
/// </summary>
public sealed class TokenClientInfo
{
    /// <summary>
    /// 来源IP
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 来源设备标识(通常是 User-Agent)
    /// </summary>
    public string? UserAgent { get; set; }
}

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
