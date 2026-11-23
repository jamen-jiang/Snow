using System.Collections.Concurrent;
using System.Dynamic;

namespace System.Collections.Generic;

/// <summary>
/// 针对字典的扩展方法。
/// </summary>
public static class SnowDictionaryExtensions
{
    /// <summary>
    /// 尝试从字典里取一个值: 键存在且类型对得上才算取到。
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="dictionary">目标字典</param>
    /// <param name="key">键</param>
    /// <param name="value">取到的值; 键不存在或类型不符时为默认值</param>
    /// <returns>取到返回 true, 否则返回 false。</returns>
    internal static bool TryGetValue<T>(this IDictionary<string, object> dictionary, string key, out T? value)
    {
        object? valueObj;
        if (dictionary.TryGetValue(key, out valueObj) && valueObj is T)
        {
            value = (T)valueObj;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 按键取值, 找不到就返回默认值。
    /// </summary>
    /// <param name="dictionary">目标字典</param>
    /// <param name="key">要查找的键</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>找到时返回值, 找不到时返回默认值。</returns>
    public static TValue? GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
    {
        TValue? obj;
        return dictionary.TryGetValue(key, out obj) ? obj : default;
    }

    /// <summary>
    /// 按键取值, 找不到就返回默认值。
    /// </summary>
    /// <param name="dictionary">目标字典</param>
    /// <param name="key">要查找的键</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>找到时返回值, 找不到时返回默认值。</returns>
    public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary.TryGetValue(key, out var obj) ? obj : default;
    }

    /// <summary>
    /// 按键取值, 找不到就返回默认值。
    /// </summary>
    /// <param name="dictionary">目标字典</param>
    /// <param name="key">要查找的键</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>找到时返回值, 找不到时返回默认值。</returns>
    public static TValue? GetOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary.TryGetValue(key, out var obj) ? obj : default;
    }

    /// <summary>
    /// 按键取值, 找不到就返回默认值。
    /// </summary>
    /// <param name="dictionary">目标字典</param>
    /// <param name="key">要查找的键</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>找到时返回值, 找不到时返回默认值。</returns>
    public static TValue? GetOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
    {
        return dictionary.TryGetValue(key, out var obj) ? obj : default;
    }

    /// <summary>
    /// 按键取值, 取不到就用 <paramref name="factory"/> 造一个存进去并返回。
    /// </summary>
    /// <param name="dictionary">目标字典</param>
    /// <param name="key">要查找的键</param>
    /// <param name="factory">键不存在时用来创建值的工厂方法</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>字典里该键对应的值。</returns>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
    {
        TValue? obj;
        if (dictionary.TryGetValue(key, out obj))
        {
            return obj;
        }

        return dictionary[key] = factory(key);
    }

    /// <summary>
    /// 按键取值, 取不到就用 <paramref name="factory"/> 造一个存进去并返回。
    /// </summary>
    /// <param name="dictionary">目标字典</param>
    /// <param name="key">要查找的键</param>
    /// <param name="factory">键不存在时用来创建值的工厂方法</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>字典里该键对应的值。</returns>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
    {
        return dictionary.GetOrAdd(key, k => factory());
    }

    /// <summary>
    /// 从并发字典按键取值, 取不到就用 <paramref name="factory"/> 造一个存进去并返回。
    /// </summary>
    /// <param name="dictionary">目标并发字典</param>
    /// <param name="key">要查找的键</param>
    /// <param name="factory">键不存在时用来创建值的工厂方法</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>字典里该键对应的值。</returns>
    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory) where TKey : notnull
    {
        return dictionary.GetOrAdd(key, k => factory());
    }

    /// <summary>
    /// 把 &lt;string,object&gt; 字典转成动态对象, 之后可在运行期增删成员。
    /// </summary>
    /// <param name="dictionary">目标字典</param>
    /// <returns>承载这些键值的 <see cref="ExpandoObject"/></returns>
    public static dynamic ConvertToDynamicObject(this Dictionary<string, object> dictionary)
    {
        var expandoObject = new ExpandoObject();
        var expendObjectCollection = (ICollection<KeyValuePair<string, object>>)expandoObject!;

        foreach (var keyValuePair in dictionary)
        {
            expendObjectCollection.Add(keyValuePair);
        }

        return expandoObject;
    }
}
