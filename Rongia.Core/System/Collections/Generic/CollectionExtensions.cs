namespace System.Collections.Generic;

/// <summary>
/// <see cref="ICollection{T}"/> 的扩展方法集合
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// 批量追加, 已存在的元素会被跳过
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">目标集合</param>
    /// <param name="items">待追加的元素</param>
    /// <exception cref="ArgumentNullException">任一参数为 null</exception>
    public static void AddIfNotContains<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(items);

        foreach (T item in items)
        {
            if (!source.Contains(item))
            {
                source.Add(item);
            }
        }
    }
}
