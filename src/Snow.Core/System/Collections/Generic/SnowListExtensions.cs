using System.Linq;
using Snow;

namespace System.Collections.Generic;

/// <summary>
/// 针对 <see cref="IList{T}"/> 的扩展方法。
/// </summary>
public static class SnowListExtensions
{
    /// <summary>
    /// 从 <paramref name="index"/> 开始把一批元素依次插入。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="index">插入起始位置</param>
    /// <param name="items">要插入的元素</param>
    public static void InsertRange<T>(this IList<T> source, int index, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            source.Insert(index++, item);
        }
    }

    /// <summary>
    /// 按条件查找第一个匹配元素的下标, 找不到返回 -1。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="selector">匹配条件</param>
    /// <returns>匹配元素的下标, 找不到为 -1。</returns>
    public static int FindIndex<T>(this IList<T> source, Predicate<T> selector)
    {
        for (var i = 0; i < source.Count; ++i)
        {
            if (selector(source[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 插到列表最前面。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="item">要插入的元素</param>
    public static void AddFirst<T>(this IList<T> source, T item)
    {
        source.Insert(0, item);
    }

    /// <summary>
    /// 加到列表最后面。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="item">要插入的元素</param>
    public static void AddLast<T>(this IList<T> source, T item)
    {
        source.Insert(source.Count, item);
    }

    /// <summary>
    /// 插到指定元素后面; 指定的元素不在列表里时插到最前面。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="existingItem">参照元素</param>
    /// <param name="item">要插入的元素</param>
    public static void InsertAfter<T>(this IList<T> source, T existingItem, T item)
    {
        var index = source.IndexOf(existingItem);
        if (index < 0)
        {
            source.AddFirst(item);
            return;
        }

        source.Insert(index + 1, item);
    }

    /// <summary>
    /// 插到第一个满足条件的元素后面; 找不到时插到最前面。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="selector">定位参照元素的条件</param>
    /// <param name="item">要插入的元素</param>
    public static void InsertAfter<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        var index = source.FindIndex(selector);
        if (index < 0)
        {
            source.AddFirst(item);
            return;
        }

        source.Insert(index + 1, item);
    }

    /// <summary>
    /// 插到指定元素前面; 指定的元素不在列表里时加到列表最后面。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="existingItem">参照元素</param>
    /// <param name="item">要插入的元素</param>
    public static void InsertBefore<T>(this IList<T> source, T existingItem, T item)
    {
        var index = source.IndexOf(existingItem);
        if (index < 0)
        {
            source.AddLast(item);
            return;
        }

        source.Insert(index, item);
    }

    /// <summary>
    /// 插到第一个满足条件的元素前面; 找不到时加到列表最后面。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="selector">定位参照元素的条件</param>
    /// <param name="item">要插入的元素</param>
    public static void InsertBefore<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        var index = source.FindIndex(selector);
        if (index < 0)
        {
            source.AddLast(item);
            return;
        }

        source.Insert(index, item);
    }

    /// <summary>
    /// 把所有满足条件的元素都替换成同一个值。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="selector">要替换哪些元素</param>
    /// <param name="item">替换成的值</param>
    public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        for (int i = 0; i < source.Count; i++)
        {
            if (selector(source[i]))
            {
                source[i] = item;
            }
        }
    }

    /// <summary>
    /// 把所有满足条件的元素逐个用工厂方法算出的新值替换。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="selector">要替换哪些元素</param>
    /// <param name="itemFactory">由旧元素算出新元素</param>
    public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
    {
        for (int i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (selector(item))
            {
                source[i] = itemFactory(item);
            }
        }
    }

    /// <summary>
    /// 只把第一个满足条件的元素替换成给定值。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="selector">要替换哪个元素</param>
    /// <param name="item">替换成的值</param>
    public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        for (int i = 0; i < source.Count; i++)
        {
            if (selector(source[i]))
            {
                source[i] = item;
                return;
            }
        }
    }

    /// <summary>
    /// 只把第一个满足条件的元素用工厂方法算出的新值替换。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="selector">要替换哪个元素</param>
    /// <param name="itemFactory">由旧元素算出新元素</param>
    public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
    {
        for (int i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (selector(item))
            {
                source[i] = itemFactory(item);
                return;
            }
        }
    }

    /// <summary>
    /// 按相等比较找到第一个与 <paramref name="item"/> 相等的元素并替换。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="item">要查找的旧值</param>
    /// <param name="replaceWith">替换成的新值</param>
    public static void ReplaceOne<T>(this IList<T> source, T item, T replaceWith)
    {
        for (int i = 0; i < source.Count; i++)
        {
            if (Comparer<T>.Default.Compare(source[i], item) == 0)
            {
                source[i] = replaceWith;
                return;
            }
        }
    }

    /// <summary>
    /// 把第一个满足条件的元素移动到指定下标处。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="selector">要移动哪个元素</param>
    /// <param name="targetIndex">移动到的目标下标, 必须在 0 到 Count-1 之间</param>
    public static void MoveItem<T>(this List<T> source, Predicate<T> selector, int targetIndex)
    {
        if (!targetIndex.IsBetween(0, source.Count - 1))
        {
            throw new IndexOutOfRangeException("targetIndex should be between 0 and " + (source.Count - 1));
        }

        var currentIndex = source.FindIndex(0, selector);
        if (currentIndex == targetIndex)
        {
            return;
        }

        var item = source[currentIndex];
        source.RemoveAt(currentIndex);
        source.Insert(targetIndex, item);
    }

    /// <summary>
    /// 按条件取元素, 取不到就用工厂方法造一个加进列表并返回。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标列表</param>
    /// <param name="selector">查找条件</param>
    /// <param name="factory">列表里没有时用来创建元素的工厂方法</param>
    /// <returns>列表里符合条件或新加进去的元素。</returns>
    public static T GetOrAdd<T>(this IList<T> source, Func<T, bool> selector, Func<T> factory)
    {
        Check.NotNull(source, nameof(source));

        var item = source.FirstOrDefault(selector);

        if (item == null)
        {
            item = factory();
            source.Add(item);
        }

        return item;
    }

    /// <summary>
    /// 按依赖关系做拓扑排序。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">要排序的元素集合</param>
    /// <param name="getDependencies">给定一个元素, 返回它依赖的元素</param>
    /// <param name="comparer">判断依赖元素是否同一个时用的比较器</param>
    /// <returns>
    /// 按依赖排好序的新列表: 如果 A 依赖 B, 结果里 B 排在 A 前面。
    /// 出现循环依赖时抛 <see cref="ArgumentException"/>。
    /// </returns>
    public static List<T> SortByDependencies<T>(
        this IEnumerable<T> source,
        Func<T, IEnumerable<T>> getDependencies,
        IEqualityComparer<T>? comparer = null) where T : notnull
    {
        /* 参考: http://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
         *       http://en.wikipedia.org/wiki/Topological_sorting
         */

        var sorted = new List<T>();
        var visited = new Dictionary<T, bool>(comparer);

        foreach (var item in source)
        {
            SortByDependenciesVisit(item, getDependencies, sorted, visited);
        }

        return sorted;
    }

    /// <summary>
    /// 拓扑排序的深度优先递归: 先处理完所有依赖, 再把当前元素放进结果列表。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="item">当前处理的元素</param>
    /// <param name="getDependencies">给定一个元素, 返回它依赖的元素</param>
    /// <param name="sorted">排好序的结果列表, 递归过程中不断追加</param>
    /// <param name="visited">已访问标记: true 表示正在递归中, false 表示已排完</param>
    private static void SortByDependenciesVisit<T>(T item, Func<T, IEnumerable<T>> getDependencies, List<T> sorted,
        Dictionary<T, bool> visited) where T : notnull
    {
        bool inProcess;
        var alreadyVisited = visited.TryGetValue(item, out inProcess);

        if (alreadyVisited)
        {
            if (inProcess)
            {
                throw new ArgumentException("Cyclic dependency found! Item: " + item);
            }
        }
        else
        {
            visited[item] = true;

            var dependencies = getDependencies(item);
            if (dependencies != null)
            {
                foreach (var dependency in dependencies)
                {
                    SortByDependenciesVisit(dependency, getDependencies, sorted, visited);
                }
            }

            visited[item] = false;
            sorted.Add(item);
        }
    }
}
