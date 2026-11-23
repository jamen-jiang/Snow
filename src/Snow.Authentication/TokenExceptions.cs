namespace Snow.Authentication;

/// <summary>
/// 登录态异常基类
/// </summary>
public class TokenException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    public TokenException() { }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public TokenException(string message) : base(message) { }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public TokenException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 未登录(或登录态已失效)异常
/// </summary>
public sealed class TokenNotLoginException : TokenException
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public TokenNotLoginException(string? message = null) : base(message ?? "未登录或登录已过期。") { }
}

/// <summary>
/// 缺少角色异常
/// </summary>
public sealed class TokenNotRoleException : TokenException
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="roles">缺失的角色</param>
    public TokenNotRoleException(params string[] roles)
        : base($"缺少角色: {string.Join(", ", roles)}") => Roles = roles;

    /// <summary>
    /// 缺失的角色
    /// </summary>
    public string[] Roles { get; }
}

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
