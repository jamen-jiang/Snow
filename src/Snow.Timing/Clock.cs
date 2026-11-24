using System;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Timing;

/// <summary>
/// 默认时间提供器(对应 ABP 的 <c>Clock</c>): 以 <see cref="SnowClockOptions.Kind"/> 为基准读系统时钟, 并按当前时区换算
/// </summary>
[TransientService]
public class Clock : IClock
{
    /// <summary>
    /// 时钟选项
    /// </summary>
    protected SnowClockOptions Options { get; }

    /// <summary>
    /// 当前时区提供器
    /// </summary>
    protected ICurrentTimezoneProvider CurrentTimezoneProvider { get; }

    /// <summary>
    /// 时区换算提供器
    /// </summary>
    protected ITimezoneProvider TimezoneProvider { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">时钟选项</param>
    /// <param name="currentTimezoneProvider">当前时区提供器</param>
    /// <param name="timezoneProvider">时区换算提供器</param>
    public Clock(
        IOptions<SnowClockOptions> options,
        ICurrentTimezoneProvider currentTimezoneProvider,
        ITimezoneProvider timezoneProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(currentTimezoneProvider);
        ArgumentNullException.ThrowIfNull(timezoneProvider);

        Options = options.Value;
        CurrentTimezoneProvider = currentTimezoneProvider;
        TimezoneProvider = timezoneProvider;
    }

    /// <inheritdoc />
    public virtual DateTime Now => Options.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;

    /// <inheritdoc />
    public virtual DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public virtual DateTimeOffset OffsetNow => DateTimeOffset.Now;

    /// <inheritdoc />
    public virtual DateTimeOffset OffsetUtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public virtual DateTimeKind Kind => Options.Kind;

    /// <inheritdoc />
    public virtual bool SupportsMultipleTimezone => Options.Kind == DateTimeKind.Utc;

    /// <inheritdoc />
    public virtual DateTime Normalize(DateTime dateTime)
    {
        if (Kind == DateTimeKind.Unspecified || Kind == dateTime.Kind)
        {
            return dateTime;
        }

        if (Kind == DateTimeKind.Local && dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime.ToLocalTime();
        }

        if (Kind == DateTimeKind.Utc && dateTime.Kind == DateTimeKind.Local)
        {
            return dateTime.ToUniversalTime();
        }

        return DateTime.SpecifyKind(dateTime, Kind);
    }

    /// <inheritdoc />
    public virtual DateTime ConvertToUserTime(DateTime utcDateTime)
    {
        if (!SupportsMultipleTimezone ||
            utcDateTime.Kind != DateTimeKind.Utc ||
            CurrentTimezoneProvider.TimeZone.IsNullOrWhiteSpace())
        {
            return utcDateTime;
        }

        TimeZoneInfo timezoneInfo = TimezoneProvider.GetTimeZoneInfo(CurrentTimezoneProvider.TimeZone);
        return TimeZoneInfo.ConvertTime(utcDateTime, timezoneInfo);
    }

    /// <inheritdoc />
    public virtual DateTimeOffset ConvertToUserTime(DateTimeOffset dateTimeOffset)
    {
        if (!SupportsMultipleTimezone ||
            CurrentTimezoneProvider.TimeZone.IsNullOrWhiteSpace())
        {
            return dateTimeOffset;
        }

        TimeZoneInfo timezoneInfo = TimezoneProvider.GetTimeZoneInfo(CurrentTimezoneProvider.TimeZone);
        return TimeZoneInfo.ConvertTime(dateTimeOffset, timezoneInfo);
    }

    /// <inheritdoc />
    public virtual DateTime ConvertToUtc(DateTime dateTime)
    {
        if (!SupportsMultipleTimezone ||
            dateTime.Kind == DateTimeKind.Utc ||
            CurrentTimezoneProvider.TimeZone.IsNullOrWhiteSpace())
        {
            return dateTime;
        }

        TimeZoneInfo timezoneInfo = TimezoneProvider.GetTimeZoneInfo(CurrentTimezoneProvider.TimeZone);
        dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(dateTime, timezoneInfo);
    }
}
