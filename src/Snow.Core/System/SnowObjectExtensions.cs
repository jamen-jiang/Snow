using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Snow.Reflection;

namespace System;

/// <summary>
/// 针对所有对象的扩展方法。
/// </summary>
public static class SnowObjectExtensions
{
    /// <summary>
    /// 把对象强制转换成目标类型, 省掉到处写括号。
    /// </summary>
    /// <typeparam name="T">要转换成的类型</typeparam>
    /// <param name="obj">要转换的对象</param>
    /// <returns>转换后的对象</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T As<T>(this object obj)
        where T : class
    {
        return (T)obj;
    }

    /// <summary>
    /// 用 <see cref="Convert.ChangeType(object,System.Type)"/> 把对象转成值类型。
    /// </summary>
    /// <param name="obj">要转换的对象</param>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>转换后的值</returns>
    public static T To<T>(this object obj)
        where T : struct
    {
        if (typeof(T) == typeof(Guid))
        {
            return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(obj.ToString()!)!;
        }

        return (T)TypeHelper.ChangeType(obj, typeof(T));
    }

    /// <summary>
    /// 判断某个值是否在一组候选值里。
    /// </summary>
    /// <param name="item">要判断的值</param>
    /// <param name="list">候选值列表</param>
    /// <typeparam name="T">值的类型</typeparam>
    public static bool IsIn<T>(this T item, params T[] list)
    {
        return list.Contains(item);
    }

    /// <summary>
    /// 判断某个值是否在给定的集合里。
    /// </summary>
    /// <param name="item">要判断的值</param>
    /// <param name="items">候选集合</param>
    /// <typeparam name="T">值的类型</typeparam>
    public static bool IsIn<T>(this T item, IEnumerable<T> items)
    {
        return items.Contains(item);
    }

    /// <summary>
    /// 条件成立时对对象做一次变换, 并返回变换后的对象或原对象; 便于链式调用。
    /// </summary>
    /// <param name="obj">原对象</param>
    /// <param name="condition">判断条件</param>
    /// <param name="func">条件为 <code>true</code> 时执行的变换</param>
    /// <typeparam name="T">对象类型</typeparam>
    /// <returns>
    /// 条件为 <code>true</code> 时返回 <paramref name="func"/> 的处理结果, 否则返回原对象。
    /// </returns>
    public static T If<T>(this T obj, bool condition, Func<T, T> func)
    {
        if (condition)
        {
            return func(obj);
        }

        return obj;
    }

    /// <summary>
    /// 条件成立时对对象执行一次操作, 并原样返回该对象; 便于链式调用。
    /// </summary>
    /// <param name="obj">原对象</param>
    /// <param name="condition">判断条件</param>
    /// <param name="action">条件为 <code>true</code> 时执行的操作</param>
    /// <typeparam name="T">对象类型</typeparam>
    /// <returns>
    /// 原对象本身。
    /// </returns>
    public static T If<T>(this T obj, bool condition, Action<T> action)
    {
        if (condition)
        {
            action(obj);
        }

        return obj;
    }
}
