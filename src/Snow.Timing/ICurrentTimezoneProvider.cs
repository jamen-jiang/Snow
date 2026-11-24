namespace Snow.Timing;

/// <summary>
/// 当前时区提供器(对应 ABP 的 <c>ICurrentTimezoneProvider</c>): 以异步流程为边界保存当前用户时区
/// </summary>
public interface ICurrentTimezoneProvider
{
    /// <summary>
    /// 当前时区(Windows 或 IANA 时区 ID), 未设置为 null
    /// </summary>
    string? TimeZone { get; set; }
}
