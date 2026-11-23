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
