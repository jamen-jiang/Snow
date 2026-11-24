using System;
using System.Collections.Generic;
using System.Linq;
using TimeZoneConverter;

namespace Snow.Timing;

/// <summary>
/// 时区展示辅助(对应 ABP 的 <c>TimeZoneHelper</c>): 给时区名拼上 UTC 偏移, 便于 UI 展示
/// </summary>
public static class TimeZoneHelper
{
    /// <summary>
    /// 把时区列表转成「名称 (偏移)」的形式
    /// </summary>
    /// <param name="timezones">时区集合</param>
    public static List<NameValue> GetTimezones(List<NameValue> timezones)
    {
        return timezones
            .OrderBy(x => x.Name)
            .Select(x => new NameValue($"{x.Name} ({GetTimezoneOffset(TZConvert.GetTimeZoneInfo(x.Name))})", x.Name))
            .ToList();
    }

    /// <summary>
    /// 取时区相对 UTC 的偏移文本, 如 <c>+08:00</c>
    /// </summary>
    /// <param name="timeZoneInfo">时区信息</param>
    public static string GetTimezoneOffset(TimeZoneInfo timeZoneInfo)
    {
        if (timeZoneInfo.BaseUtcOffset < TimeSpan.Zero)
        {
            return "-" + timeZoneInfo.BaseUtcOffset.ToString(@"hh:mm");
        }

        return "+" + timeZoneInfo.BaseUtcOffset.ToString(@"hh:mm");
    }
}
