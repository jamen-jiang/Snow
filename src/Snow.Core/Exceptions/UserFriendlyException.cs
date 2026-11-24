using Microsoft.Extensions.Logging;

namespace Snow.Exceptions;

/// <summary>
/// 可以直接展示给调用方的业务异常(对应 ABP 的 <c>UserFriendlyException</c>)
/// <para>抛出它表示"这次失败是业务规则不允许", 消息本身就可以返回给客户端; 与程序缺陷导致的异常不同。</para>
/// <para>构造器与 ABP 同形: 消息必填, 其余可选(<c>code</c> / <c>details</c> / <c>innerException</c> / <c>logLevel</c>)。</para>
/// </summary>
public class UserFriendlyException : BusinessException, IUserFriendlyException
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">可直接展示给调用方的消息</param>
    /// <param name="code">业务错误码</param>
    /// <param name="details">错误说明文本</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="logLevel">日志级别, 默认 Warning</param>
    public UserFriendlyException(
        string message,
        string? code = null,
        string? details = null,
        Exception? innerException = null,
        LogLevel logLevel = LogLevel.Warning)
        : base(code, message, details, innerException, logLevel)
    {
    }
}
