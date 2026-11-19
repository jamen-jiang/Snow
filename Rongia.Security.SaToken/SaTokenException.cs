namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态异常基类
/// </summary>
public class SaTokenException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    public SaTokenException() { }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public SaTokenException(string message) : base(message) { }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public SaTokenException(string message, Exception innerException) : base(message, innerException) { }
}
