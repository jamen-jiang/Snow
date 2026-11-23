namespace System;

/// <summary>
/// 针对 <see cref="IComparable{T}"/> 的扩展方法。
/// </summary>
public static class SnowComparableExtensions
{
    /// <summary>
    /// 判断一个值是否落在最小值与最大值之间(两端都包含)。
    /// </summary>
    /// <param name="value">要判断的值</param>
    /// <param name="minInclusiveValue">最小值(含)</param>
    /// <param name="maxInclusiveValue">最大值(含)</param>
    public static bool IsBetween<T>(this T value, T minInclusiveValue, T maxInclusiveValue) where T : IComparable<T>
    {
        return value.CompareTo(minInclusiveValue) >= 0 && value.CompareTo(maxInclusiveValue) <= 0;
    }
}
