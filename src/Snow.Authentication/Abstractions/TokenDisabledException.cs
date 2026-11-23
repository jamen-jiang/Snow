namespace Snow.Authentication;

/// <summary>
/// 账号被封禁异常
/// </summary>
public sealed class TokenDisabledException : TokenException
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="loginId">登录Id</param>
    /// <param name="remaining">剩余封禁时长, <see cref="TimeSpan.MaxValue"/> 表示永久</param>
    public TokenDisabledException(long loginId, TimeSpan remaining)
        : base($"账号 {loginId} 已被封禁, 剩余 {(remaining == TimeSpan.MaxValue ? "永久" : remaining.ToString())}。")
    {
        LoginId = loginId;
        Remaining = remaining;
    }

    /// <summary>
    /// 登录Id
    /// </summary>
    public long LoginId { get; }

    /// <summary>
    /// 剩余封禁时长
    /// </summary>
    public TimeSpan Remaining { get; }
}
