using Snow.Exceptions;

namespace Snow;

/// <summary>
/// 可以直接展示给调用方的业务异常(对应 ABP 的 <c>AbpUserFriendlyException</c>)
/// <para>抛出它表示"这次失败是业务规则不允许", 消息本身就可以返回给客户端; 与程序缺陷导致的异常不同。</para>
/// <para>继承 <see cref="SnowBusinessException"/>(日志级别 Warning、错误码 + 附加数据), 只是语义上承诺消息可直接展示。</para>
/// </summary>
public class SnowUserFriendlyException : SnowBusinessException
{
    /// <summary>
    /// 用消息构造
    /// </summary>
    /// <param name="message">可直接展示给调用方的消息</param>
    public SnowUserFriendlyException(string message)
        : base(message: message)
    {
    }

    /// <summary>
    /// 用错误码与消息构造
    /// </summary>
    /// <param name="code">业务错误码</param>
    /// <param name="message">可直接展示给调用方的消息</param>
    public SnowUserFriendlyException(string code, string message)
        : base(code: code, message: message)
    {
    }

    /// <summary>
    /// 用消息与内部异常构造
    /// </summary>
    /// <param name="message">可直接展示给调用方的消息</param>
    /// <param name="innerException">内部异常</param>
    public SnowUserFriendlyException(string message, Exception innerException)
        : base(message: message, innerException: innerException)
    {
    }
}
