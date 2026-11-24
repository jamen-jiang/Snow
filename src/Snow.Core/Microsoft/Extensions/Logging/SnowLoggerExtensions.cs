using System;
using System.Text;
using System.Text.Json;
using Snow.Logging;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// <see cref="ILogger"/> 扩展(对应 ABP 的 <c>AbpLoggerExtensions</c>): 按级别写日志, 以及把异常连同已知信息一并记录。
/// </summary>
public static class SnowLoggerExtensions
{
    /// <summary>
    /// 按指定级别写一条消息。
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="logLevel">日志级别</param>
    /// <param name="message">消息</param>
    public static void LogWithLevel(this ILogger logger, LogLevel logLevel, string message)
    {
        switch (logLevel)
        {
            case LogLevel.Critical: logger.LogCritical(message); break;
            case LogLevel.Error: logger.LogError(message); break;
            case LogLevel.Warning: logger.LogWarning(message); break;
            case LogLevel.Information: logger.LogInformation(message); break;
            case LogLevel.Trace: logger.LogTrace(message); break;
            default: logger.LogDebug(message); break;
        }
    }

    /// <summary>
    /// 按指定级别写一条带异常的消息。
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="logLevel">日志级别</param>
    /// <param name="message">消息</param>
    /// <param name="exception">异常</param>
    public static void LogWithLevel(this ILogger logger, LogLevel logLevel, string message, Exception exception)
    {
        switch (logLevel)
        {
            case LogLevel.Critical: logger.LogCritical(exception, message); break;
            case LogLevel.Error: logger.LogError(exception, message); break;
            case LogLevel.Warning: logger.LogWarning(exception, message); break;
            case LogLevel.Information: logger.LogInformation(exception, message); break;
            case LogLevel.Trace: logger.LogTrace(exception, message); break;
            default: logger.LogDebug(exception, message); break;
        }
    }

    /// <summary>
    /// 记录一个异常: 级别默认取 <see cref="IHasLogLevel.LogLevel"/>, 否则按 Error。
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="ex">异常</param>
    /// <param name="level">日志级别, 可为 null</param>
    public static void LogException(this ILogger logger, Exception ex, LogLevel? level = null)
    {
        LogLevel selectedLevel = level ?? (ex is IHasLogLevel hasLogLevel ? hasLogLevel.LogLevel : LogLevel.Error);

        logger.LogWithLevel(selectedLevel, ex.Message, ex);
        LogData(logger, ex, selectedLevel);
    }

    private static void LogData(ILogger logger, Exception exception, LogLevel logLevel)
    {
        if (exception.Data.Count <= 0)
        {
            return;
        }

        var exceptionData = new StringBuilder();
        exceptionData.AppendLine("---------- Exception Data ----------");
        foreach (object key in exception.Data.Keys)
        {
            exceptionData.AppendLine($"{key} = {FormatDataValue(exception.Data[key])}");
        }

        logger.LogWithLevel(logLevel, exceptionData.ToString());
    }

    private const int MaxDataValueLength = 4096;

    private static string FormatDataValue(object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        Type type = value.GetType();
        if (value is string || type.IsPrimitive || value is decimal || value is DateTime || value is DateTimeOffset || value is Guid || type.IsEnum)
        {
            return value.ToString()!;
        }

        try
        {
            return JsonSerializer.Serialize(value).TruncateWithPostfix(MaxDataValueLength, "...(truncated)")!;
        }
        catch
        {
            return value.ToString()!;
        }
    }
}
