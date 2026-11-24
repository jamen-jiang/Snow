using Microsoft.Extensions.Logging;

namespace Snow.Exceptions;

/// <summary>
/// 业务规则不允许时抛出的异常(对应 ABP 的 <c>BusinessException</c>)
/// <para>与 <see cref="Snow.SnowUserFriendlyException"/> 的关系: 后者是它的子类, 消息一定返回给客户端;
/// 本类只保证错误码返回给客户端, 消息默认也返回(业务失败的说明通常不是机密), 附加数据只在
/// <c>SendExceptionDetailsToClients</c> 打开时返回。</para>
/// <para>日志级别默认 Warning: 业务失败是预期内的, 不该按 Error 刷日志。</para>
/// </summary>
public class SnowBusinessException : SnowException, IHasErrorDetails
{
    /// <summary>
    /// 附加数据, 会原样序列化进错误响应
    /// </summary>
    public object? Details { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowBusinessException()
    {
        LogLevel = LogLevel.Warning;
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="code">业务错误码</param>
    /// <param name="message">异常消息</param>
    /// <param name="details">附加数据</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="logLevel">日志级别, 默认 Warning</param>
    public SnowBusinessException(
        string? code = null,
        string? message = null,
        object? details = null,
        Exception? innerException = null,
        LogLevel logLevel = LogLevel.Warning)
        : base(message, innerException, logLevel)
    {
        Code = code;
        Details = details;
    }
}