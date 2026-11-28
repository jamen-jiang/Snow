using Snow.Exceptions;

namespace Snow.AspNetCore.Authentication;

/// <summary>
/// 登录态异常基类
/// </summary>
public class TokenException : SnowException
{
    /// <summary>
    /// 构造器
    /// </summary>
    public TokenException() { }

    /// <summary>
    /// 构造器
    /// </summary>
    public TokenException(string? message) : base(message) { }

    /// <summary>
    /// 构造器
    /// </summary>
    public TokenException(string? message, Exception? innerException) : base(message, innerException) { }
}
