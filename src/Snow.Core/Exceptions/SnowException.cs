using System;

namespace Snow.Exceptions;

/// <summary>
/// Snow 框架异常树的根(对应 ABP 的 <c>AbpException</c>): 与基类一样只有三个构造器, 不携带错误码与日志级别。
/// <para>需要错误码或日志级别的异常自己实现 <c>IHasErrorCode</c>(<c>Code</c>)与 <c>IHasLogLevel</c>(<c>LogLevel</c>),
/// 取日志级别用 <c>SnowExceptionExtensions.GetLogLevel()</c> 扩展方法(ABP 同款)。
/// 业务规则不允许的失败请用 <c>BusinessException</c>(业务异常树的根, 直接继承 <c>Exception</c>, 与本类平行)。</para>
/// </summary>
public class SnowException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    public SnowException()
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public SnowException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public SnowException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
