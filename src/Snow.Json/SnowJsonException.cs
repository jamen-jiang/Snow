using Snow.Exceptions;

namespace Snow.Json;

/// <summary>
/// JSON 处理异常: 序列化或反序列化失败时抛出
/// </summary>
public class SnowJsonException : SnowException
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public SnowJsonException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public SnowJsonException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}