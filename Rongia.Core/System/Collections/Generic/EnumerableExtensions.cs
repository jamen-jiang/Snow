using System;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Generic;

/// <summary>
/// <see cref="IEnumerable{T}"/> 的扩展方法集合
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// 用指定分隔符拼接字符串集合,等价于 string.Join(...)
    /// </summary>
    /// <param name="source">待拼接的字符串集合</param>
    /// <param name="separator">分隔符;仅当集合元素多于一个时才会出现在结果中</param>
    /// <returns>以分隔符连接的字符串;集合为空时返回 <see cref="string.Empty"/></returns>
    public static string JoinAsString(this IEnumerable<string> source, string separator)
    {
        return string.Join(separator, source);
    }

    /// <summary>
    /// 用指定分隔符拼接任意类型集合,等价于 string.Join(...)
    /// </summary>
    /// <param name="source">待拼接的集合</param>
    /// <param name="separator">分隔符;仅当集合元素多于一个时才会出现在结果中</param>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <returns>以分隔符连接的字符串;集合为空时返回 <see cref="string.Empty"/></returns>
    public static string JoinAsString<T>(this IEnumerable<T> source, string separator)
    {
        return string.Join(separator, source);
    }

    /// <summary>
    /// 条件式过滤:仅当条件成立时按谓词过滤,否则原样返回
    /// </summary>
    /// <param name="source">待过滤的序列</param>
    /// <param name="condition">是否执行过滤</param>
    /// <param name="predicate">过滤谓词</param>
    /// <returns>依据 <paramref name="condition"/> 决定是否过滤后的序列</returns>
    public static IEnumerable<T> WhereIf<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<T, bool> predicate
    )
    {
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>
    /// 条件式过滤(带索引):仅当条件成立时按谓词过滤,否则原样返回
    /// </summary>
    /// <param name="source">待过滤的序列</param>
    /// <param name="condition">是否执行过滤</param>
    /// <param name="predicate">过滤谓词,第二个参数为元素索引</param>
    /// <returns>依据 <paramref name="condition"/> 决定是否过滤后的序列</returns>
    public static IEnumerable<T> WhereIf<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<T, int, bool> predicate
    )
    {
        return condition ? source.Where(predicate) : source;
    }
}
