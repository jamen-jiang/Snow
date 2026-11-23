namespace System;

/// <summary>
/// 针对 <see cref="DateTime"/> 的扩展方法。
/// </summary>
public static class SnowDateTimeExtensions
{
    /// <summary>
    /// 清掉时间部分, 只保留日期(时、分、秒、毫秒全部归零)。
    /// </summary>
    /// <param name="dateTime">要处理的日期时间</param>
    /// <returns>只保留日期部分的 <see cref="DateTime"/></returns>
    public static DateTime ClearTime(this DateTime dateTime)
    {
        return dateTime.Subtract(
            new TimeSpan(
                0,
                dateTime.Hour,
                dateTime.Minute,
                dateTime.Second,
                dateTime.Millisecond
            )
        );
    }
}
