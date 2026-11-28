using System;
using Microsoft.Extensions.Logging;
using Snow.Logging;

namespace Snow.Exceptions;

/// <summary>
/// 数据库并发冲突异常(对应 ABP 的 <c>AbpDbConcurrencyException</c>): 并发更新同一行失败时抛出, 映射为 409。
/// </summary>
public class SnowDbConcurrencyException : SnowException, IHasLogLevel
{
    /// <summary>
    /// 日志级别: 并发冲突是预期内的, 记 Warning
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowDbConcurrencyException()
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public SnowDbConcurrencyException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public SnowDbConcurrencyException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
