namespace System;

/// <summary>
/// 针对 <see cref="DayOfWeek"/> 的扩展方法。
/// </summary>
public static class SnowDayOfWeekExtensions
{
    /// <summary>
    /// 判断给定的 <see cref="DayOfWeek"/> 是否为周末(周六、周日)。
    /// </summary>
    public static bool IsWeekend(this DayOfWeek dayOfWeek)
    {
        return dayOfWeek.IsIn(DayOfWeek.Saturday, DayOfWeek.Sunday);
    }

    /// <summary>
    /// 判断给定的 <see cref="DayOfWeek"/> 是否为工作日(周一至周五)。
    /// </summary>
    public static bool IsWeekday(this DayOfWeek dayOfWeek)
    {
        return dayOfWeek.IsIn(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);
    }
}
