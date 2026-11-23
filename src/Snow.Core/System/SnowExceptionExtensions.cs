using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Snow.Logging;

namespace System;

/// <summary>
/// 针对 <see cref="Exception"/> 的扩展方法。
/// </summary>
public static class SnowExceptionExtensions
{
    /// <summary>
    /// 用 <see cref="ExceptionDispatchInfo.Capture"/> 重新抛出异常, 同时保留原始堆栈。
    /// </summary>
    /// <param name="exception">要重新抛出的异常</param>
    public static void ReThrow(this Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    /// <summary>
    /// 尝试从异常自身取日志级别: 异常实现了 <see cref="IHasLogLevel"/> 就用它声明的级别,
    /// 否则返回 <paramref name="defaultLevel"/>。
    /// </summary>
    /// <param name="exception">要判断的异常</param>
    /// <param name="defaultLevel">异常没有声明级别时使用的默认级别</param>
    /// <returns>记录这条异常时该用的日志级别</returns>
    public static LogLevel GetLogLevel(this Exception exception, LogLevel defaultLevel = LogLevel.Error)
    {
        return (exception as IHasLogLevel)?.LogLevel ?? defaultLevel;
    }
}
