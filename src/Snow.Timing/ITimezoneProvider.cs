using System;
using System.Collections.Generic;

namespace Snow.Timing;

/// <summary>
/// 时区换算提供器(对应 ABP 的 <c>ITimezoneProvider</c>): 提供 Windows 与 IANA 时区 ID 的互转与时区信息查询
/// </summary>
public interface ITimezoneProvider
{
    /// <summary>
    /// 取全部 Windows 时区 ID
    /// </summary>
    List<NameValue> GetWindowsTimezones();

    /// <summary>
    /// 取全部 IANA 时区名
    /// </summary>
    List<NameValue> GetIanaTimezones();

    /// <summary>
    /// Windows 时区 ID 转 IANA 时区名
    /// </summary>
    /// <param name="windowsTimeZoneId">Windows 时区 ID</param>
    string WindowsToIana(string windowsTimeZoneId);

    /// <summary>
    /// IANA 时区名转 Windows 时区 ID
    /// </summary>
    /// <param name="ianaTimeZoneName">IANA 时区名</param>
    string IanaToWindows(string ianaTimeZoneName);

    /// <summary>
    /// 按 Windows 或 IANA 时区 ID 取 <see cref="TimeZoneInfo"/>
    /// </summary>
    /// <param name="windowsOrIanaTimeZoneId">Windows 或 IANA 时区 ID</param>
    TimeZoneInfo GetTimeZoneInfo(string windowsOrIanaTimeZoneId);
}
