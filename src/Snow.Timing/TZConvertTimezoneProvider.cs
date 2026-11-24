using System;
using System.Collections.Generic;
using System.Linq;
using TimeZoneConverter;
using Snow.DependencyInjection;

namespace Snow.Timing;

/// <summary>
/// 基于 TimeZoneConverter 库的时区换算实现(对应 ABP 的 <c>TZConvertTimezoneProvider</c>): 跨平台解析 Windows/IANA 时区
/// </summary>
[TransientService]
public class TZConvertTimezoneProvider : ITimezoneProvider
{
    /// <inheritdoc />
    public virtual List<NameValue> GetWindowsTimezones()
    {
        return TZConvert.KnownWindowsTimeZoneIds
            .OrderBy(x => x)
            .Select(x => new NameValue(x, x))
            .ToList();
    }

    /// <inheritdoc />
    public virtual List<NameValue> GetIanaTimezones()
    {
        return TZConvert.KnownIanaTimeZoneNames
            .OrderBy(x => x)
            .Where(x => x.Contains('/') && !x.Contains("Etc") || x == "UTC")
            .Select(x => new NameValue(x, x))
            .ToList();
    }

    /// <inheritdoc />
    public virtual string WindowsToIana(string windowsTimeZoneId)
    {
        return TZConvert.WindowsToIana(windowsTimeZoneId);
    }

    /// <inheritdoc />
    public virtual string IanaToWindows(string ianaTimeZoneName)
    {
        return TZConvert.IanaToWindows(ianaTimeZoneName);
    }

    /// <inheritdoc />
    public virtual TimeZoneInfo GetTimeZoneInfo(string windowsOrIanaTimeZoneId)
    {
        return TZConvert.GetTimeZoneInfo(windowsOrIanaTimeZoneId);
    }
}
