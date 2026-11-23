using System.Linq;
using Snow;

namespace System.Collections.Generic;

/// <summary>
/// 针对集合的扩展方法。
/// </summary>
public static class SnowCollectionExtensions
{
    /// <summary>
    /// 判断集合为 null 或者里面没有任何元素。
    /// </summary>
    public static bool IsNullOrEmpty<T>(this ICollection<T>? source)
    {
        return source == null || source.Count <= 0;
    }

    /// <summary>
    /// 集合里没有这个元素时才加进去。
    /// </summary>
    /// <param name="source">目标集合</param>
    /// <param name="item">要检查并添加的元素</param>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <returns>确实添加了返回 true, 已经存在返回 false。</returns>
    public static bool AddIfNotContains<T>(this ICollection<T> source, T item)
    {
        Check.NotNull(source, nameof(source));

        if (source.Contains(item))
        {
            return false;
        }

        source.Add(item);
        return true;
    }

    /// <summary>
    /// 把集合里还没有的元素批量加进去。
    /// </summary>
    /// <param name="source">目标集合</param>
    /// <param name="items">要检查并添加的元素</param>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <returns>本次真正被添加进去的元素。</returns>
    public static IEnumerable<T> AddIfNotContains<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        Check.NotNull(source, nameof(source));

        var addedItems = new List<T>();

        foreach (var item in items)
        {
            if (source.Contains(item))
            {
                continue;
            }

            source.Add(item);
            addedItems.Add(item);
        }

        return addedItems;
    }

    /// <summary>
    /// 用 <paramref name="predicate"/> 判断集合里是否已有等价元素, 没有才用 <paramref name="itemFactory"/> 造一个加进去。
    /// </summary>
    /// <param name="source">目标集合</param>
    /// <param name="predicate">判断"集合里是否已有这个元素"的条件</param>
    /// <param name="itemFactory">真正要添加的元素由它创建</param>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <returns>确实添加了返回 true, 已经存在返回 false。</returns>
    public static bool AddIfNotContains<T>(this ICollection<T> source, Func<T, bool> predicate, Func<T> itemFactory)
    {
        Check.NotNull(source, nameof(source));
        Check.NotNull(predicate, nameof(predicate));
        Check.NotNull(itemFactory, nameof(itemFactory));

        if (source.Any(predicate))
        {
            return false;
        }

        source.Add(itemFactory());
        return true;
    }

    /// <summary>
    /// 删除集合里所有满足 <paramref name="predicate"/> 的元素。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="source">目标集合</param>
    /// <param name="predicate">删除条件</param>
    /// <returns>被删掉的元素列表</returns>
    public static IList<T> RemoveAll<T>(this ICollection<T> source, Func<T, bool> predicate)
    {
        var items = source.Where(predicate).ToList();

        foreach (var item in items)
        {
            source.Remove(item);
        }

        return items;
    }

    /// <summary>
    /// 按给定元素批量删除。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="source">目标集合</param>
    /// <param name="items">要删除的元素</param>
    public static void RemoveAll<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            source.Remove(item);
        }
    }
}
