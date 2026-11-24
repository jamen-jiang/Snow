using System;

namespace Snow.Timing;

/// <summary>
/// 时间提供器(对应 ABP 的 <c>IClock</c>): 框架内所有取当前时间的代码都走这里, 便于替换成可控时钟做测试
/// </summary>
public interface IClock
{
    /// <summary>
    /// 本地时间(基准为 <see cref="SnowClockOptions.Kind"/> 时若为 UTC 则返回 UTC)
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
    /// 时钟基准 Kind
    /// </summary>
    DateTimeKind Kind { get; }

    /// <summary>
    /// 是否支持多时区(基准为 UTC 时为真)
    /// </summary>
    bool SupportsMultipleTimezone { get; }

    /// <summary>
    /// 按 Kind 归一化时间
    /// </summary>
    /// <param name="dateTime">待归一化的时间</param>
    /// <returns>归一化后的时间</returns>
    DateTime Normalize(DateTime dateTime);

    /// <summary>
    /// 把 UTC 时间转成用户所在时区
    /// </summary>
    /// <param name="utcDateTime">UTC 时间</param>
    /// <returns>转换后的时间</returns>
    DateTime ConvertToUserTime(DateTime utcDateTime);

    /// <summary>
    /// 把 UTC 时间转成用户所在时区
    /// </summary>
    /// <param name="dateTimeOffset">UTC 时间</param>
    /// <returns>转换后的时间</returns>
    DateTimeOffset ConvertToUserTime(DateTimeOffset dateTimeOffset);

    /// <summary>
    /// 把用户所在时区的时间转成 UTC
    /// </summary>
    /// <param name="dateTime">待转换的时间</param>
    /// <returns>转换后的时间</returns>
    DateTime ConvertToUtc(DateTime dateTime);
}
