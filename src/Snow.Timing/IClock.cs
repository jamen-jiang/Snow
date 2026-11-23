using System;

namespace Snow.Timing;

/// <summary>
/// 时间提供器: 框架内所有取当前时间的代码都走这里, 便于替换成可控时钟做测试
/// </summary>
public interface IClock
{
    /// <summary>
    /// 本地时间
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// UTC 时间
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// 带偏移的本地时间
    /// </summary>
    DateTimeOffset OffsetNow { get; }

    /// <summary>
    /// 带偏移的 UTC 时间
    /// </summary>
    DateTimeOffset OffsetUtcNow { get; }

    /// <summary>
    /// 按 Kind 归一化时间: 未指定 Kind 时视为 UTC
    /// </summary>
    /// <param name="dateTime">待归一化的时间</param>
    /// <returns>归一化后的时间</returns>
    DateTime Normalize(DateTime dateTime);
}
