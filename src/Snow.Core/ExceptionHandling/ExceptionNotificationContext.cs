using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Snow.ExceptionHandling;

/// <summary>
/// 异常通知上下文(对应 ABP 的 <c>ExceptionNotificationContext</c>): 携带异常、日志级别与是否已被处理。
/// </summary>
public class ExceptionNotificationContext
{
    /// <summary>
    /// 异常对象
    /// </summary>
    [NotNull]
    public Exception Exception { get; }

    /// <summary>
    /// 记录这条异常时应使用的日志级别
    /// </summary>
    public LogLevel LogLevel { get; }

    /// <summary>
    /// 是否已被处理
    /// </summary>
    public bool Handled { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <param name="logLevel">日志级别, 不传则取异常自身声明的级别</param>
    /// <param name="handled">是否已被处理</param>
    public ExceptionNotificationContext(
        [NotNull] Exception exception,
        LogLevel? logLevel = null,
        bool handled = true)
    {
        Exception = Check.NotNull(exception, nameof(exception));
        LogLevel = logLevel ?? exception.GetLogLevel();
        Handled = handled;
    }
}
