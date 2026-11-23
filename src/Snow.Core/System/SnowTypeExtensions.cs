using System.Collections.Generic;
using Snow;

namespace System;

/// <summary>
/// 针对 <see cref="Type"/> 的扩展方法。
/// </summary>
public static class SnowTypeExtensions
{
    /// <summary>
    /// 取带程序集名的类型全名, 形如 "命名空间.类型, 程序集名"。
    /// </summary>
    /// <param name="type">要取名的类型</param>
    /// <returns>类型全名 + 程序集名</returns>
    public static string GetFullNameWithAssemblyName(this Type type)
    {
        return type.FullName + ", " + type.Assembly.GetName().Name;
    }

    /// <summary>
    /// 判断本类型的实例能否赋值给 <typeparamref name="TTarget"/> 的实例。
    /// 内部使用 <see cref="Type.IsAssignableFrom"/> (判断方向相反)。
    /// </summary>
    /// <typeparam name="TTarget">目标类型</typeparam>
    public static bool IsAssignableTo<TTarget>(this Type type)
    {
        Check.NotNull(type, nameof(type));

        return type.IsAssignableTo(typeof(TTarget));
    }

    /// <summary>
    /// 判断本类型的实例能否赋值给 <paramref name="targetType"/> 的实例。
    /// 内部使用 <see cref="Type.IsAssignableFrom"/> (判断方向相反)。
    /// </summary>
    /// <param name="type">本类型</param>
    /// <param name="targetType">目标类型</param>
    public static bool IsAssignableTo(this Type type, Type targetType)
    {
        Check.NotNull(type, nameof(type));
        Check.NotNull(targetType, nameof(targetType));

        return targetType.IsAssignableFrom(type);
    }

    /// <summary>
    /// 取本类型的全部基类, 由浅到深排列。
    /// </summary>
    /// <param name="type">要查基类的类型</param>
    /// <param name="includeObject">是否把 <see cref="object"/> 也放进结果</param>
    public static Type[] GetBaseClasses(this Type type, bool includeObject = true)
    {
        Check.NotNull(type, nameof(type));

        var types = new List<Type>();
        AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject);
        return types.ToArray();
    }

    /// <summary>
    /// 取本类型的基类, 向上走到 <paramref name="stoppingType"/> 就停(该类型本身仍包含在结果里)。
    /// </summary>
    /// <param name="type">要查基类的类型</param>
    /// <param name="stoppingType">停止上溯的类型, 该类型会包含在返回结果中</param>
    /// <param name="includeObject">是否把 <see cref="object"/> 也放进结果</param>
    public static Type[] GetBaseClasses(this Type type, Type stoppingType, bool includeObject = true)
    {
        Check.NotNull(type, nameof(type));

        var types = new List<Type>();
        AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
        return types.ToArray();
    }

    private static void AddTypeAndBaseTypesRecursively(
        List<Type> types,
        Type? type,
        bool includeObject,
        Type? stoppingType = null)
    {
        if (type == null || type == stoppingType)
        {
            return;
        }

        if (!includeObject && type == typeof(object))
        {
            return;
        }

        AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
        types.Add(type);
    }
}
