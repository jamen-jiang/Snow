using System;

namespace Snow.Timing;

/// <summary>
/// <see cref="ITimezoneProvider"/> 的扩展方法(对应 ABP 的 <c>TimezoneProviderExtensions</c>)。
/// </summary>
public static class TimezoneProviderExtensions
{
    /// <summary>
    /// 把 <paramref name="dateTime"/> 当作 <paramref name="windowsOrIanaTimeZoneId"/> 时区的本地时间并转换为 UTC。
    /// </summary>
    /// <param name="timezoneProvider">时区提供器</param>
    /// <param name="dateTime">本地时间</param>
    /// <param name="windowsOrIanaTimeZoneId">Windows 或 IANA 时区 ID</param>
    /// <returns>转换后的 UTC 时间; 若越界则原值返回</returns>
    public static DateTime ConvertUnspecifiedToUtc(this ITimezoneProvider timezoneProvider, DateTime dateTime, string windowsOrIanaTimeZoneId)
    {
        ArgumentNullException.ThrowIfNull(timezoneProvider);

        TimeZoneInfo timezoneInfo = timezoneProvider.GetTimeZoneInfo(windowsOrIanaTimeZoneId);
        long utcTicks = dateTime.Ticks - timezoneInfo.GetUtcOffset(dateTime).Ticks;
        if (utcTicks < DateTime.MinValue.Ticks || utcTicks > DateTime.MaxValue.Ticks)
        {
            return dateTime;
        }

        return new DateTime(utcTicks, DateTimeKind.Utc);
    }
}
