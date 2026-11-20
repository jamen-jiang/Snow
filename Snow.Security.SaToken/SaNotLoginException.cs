namespace Snow.Security.SaToken;

/// <summary>
/// 未登录(或登录态已失效)异常
/// </summary>
public sealed class SaNotLoginException : SaTokenException
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public SaNotLoginException(string? message = null) : base(message ?? "未登录或登录已过期。") { }
}
