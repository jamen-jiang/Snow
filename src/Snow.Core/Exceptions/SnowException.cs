using Microsoft.Extensions.Logging;
using Snow.Logging;

namespace Snow.Exceptions;

/// <summary>
/// Snow 框架所有自定义异常的基类(对应 ABP 的 <c>AbpException</c>)
/// <para>统一携带错误码(<see cref="IHasErrorCode"/>)与日志级别(<see cref="IHasLogLevel"/>)。
/// 默认 <see cref="LogLevel"/> 为 Error: 说明是程序缺陷或外部依赖故障;
/// 业务规则不允许的失败请用 <see cref="SnowBusinessException"/>(默认 Warning)。</para>
/// </summary>
public class SnowException : Exception, IHasErrorCode, IHasLogLevel
{
    /// <summary>
    /// 错误码, 客户端据此分支处理
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 异常处理层写日志用的级别
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Error;

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

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="logLevel">日志级别</param>
    public SnowException(string? message, Exception? innerException, LogLevel logLevel)
        : base(message, innerException)
    {
        LogLevel = logLevel;
    }
}
