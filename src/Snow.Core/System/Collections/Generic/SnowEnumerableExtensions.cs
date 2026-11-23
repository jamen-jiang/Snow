using System.Linq;

namespace System.Collections.Generic;

/// <summary>
/// 针对 <see cref="IEnumerable{T}"/> 的扩展方法。
/// </summary>
public static class SnowEnumerableExtensions
{
    /// <summary>
    /// 用指定分隔符把一个字符串集合拼成一个字符串, 等价于 string.Join(...)。
    /// </summary>
    /// <param name="source">要拼接的字符串集合</param>
    /// <param name="separator">分隔符: 只有集合里多于一个元素时才会出现在结果中</param>
    /// <returns>用分隔符连接后的字符串; 集合为空时返回 <see cref="string.Empty"/>。</returns>
    public static string JoinAsString(this IEnumerable<string> source, string separator)
    {
        return string.Join(separator, source);
    }

    /// <summary>
    /// 用指定分隔符把集合元素拼成一个字符串, 等价于 string.Join(...)。
    /// </summary>
    /// <param name="source">要拼接的集合</param>
    /// <param name="separator">分隔符: 只有集合里多于一个元素时才会出现在结果中</param>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <returns>用分隔符连接后的字符串; 集合为空时返回 <see cref="string.Empty"/>。</returns>
    public static string JoinAsString<T>(this IEnumerable<T> source, string separator)
    {
        return string.Join(separator, source);
    }

    /// <summary>
    /// 只在 <paramref name="condition"/> 为 true 时过滤集合, 否则原样返回。
    /// </summary>
    /// <param name="source">要过滤的集合</param>
    /// <param name="condition">是否执行过滤</param>
    /// <param name="predicate">过滤条件</param>
    /// <returns>条件为 true 时返回过滤结果, 否则返回原集合</returns>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        return condition
            ? source.Where(predicate)
            : source;
    }

    /// <summary>
    /// 只在 <paramref name="condition"/> 为 true 时过滤集合, 否则原样返回。
    /// </summary>
    /// <param name="source">要过滤的集合</param>
    /// <param name="condition">是否执行过滤</param>
    /// <param name="predicate">带元素下标的过滤条件</param>
    /// <returns>条件为 true 时返回过滤结果, 否则返回原集合</returns>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, int, bool> predicate)
    {
        return condition
            ? source.Where(predicate)
            : source;
    }
}
