using Snow.Exceptions;

namespace Snow.AspNetCore.Authentication;

/// <summary>
/// 账号被封禁异常: 由全局异常中间件按 403 回给调用方
/// </summary>
public sealed class TokenDisabledException : TokenException, IHasHttpStatusCode
{
    /// <summary>
    /// 构造器
    /// </summary>
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

    /// <inheritdoc />
    public int HttpStatusCode => 403;
}
