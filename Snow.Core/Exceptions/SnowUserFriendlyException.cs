namespace Snow;

/// <summary>
/// 可以直接展示给调用方的业务异常(对应 ABP 的 AbpUserFriendlyException)
/// <para>抛出它表示"这次失败是业务规则不允许", 消息本身就可以返回给客户端; 与程序缺陷导致的异常不同。</para>
/// </summary>
public class SnowUserFriendlyException : Exception
{
    /// <summary>
    /// 业务错误码, 客户端据此区分具体失败原因
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 附加数据, 会原样序列化进错误响应
    /// </summary>
    public object? Details { get; set; }

    /// <summary>
    /// 用消息构造
    /// </summary>
    /// <param name="message">可直接展示给调用方的消息</param>
    public SnowUserFriendlyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 用错误码与消息构造
    /// </summary>
    /// <param name="code">业务错误码</param>
    /// <param name="message">可直接展示给调用方的消息</param>
    public SnowUserFriendlyException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// 用消息与内部异常构造
    /// </summary>
    /// <param name="message">可直接展示给调用方的消息</param>
    /// <param name="innerException">内部异常</param>
    public SnowUserFriendlyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
