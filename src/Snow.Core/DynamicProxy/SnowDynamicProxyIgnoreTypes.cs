using System;
using System.Collections.Generic;
using System.Linq;

namespace Snow.DynamicProxy;

/// <summary>
/// 不做动态代理的类型清单。
/// <para>Castle 的类代理对部分组件(例如 ASP.NET Core MVC 的 Controller)有性能开销: 框架为了工作单元、审计、授权等能力
/// 给他们开代理后, 应用性能会下降。这类组件可以改用中间件或 MVC/Page 过滤器实现同样的能力, 于是把它们的类型加进这里跳过代理。</para>
/// <para>默认是空集合, 需要时再加。</para>
/// </summary>
public static class SnowDynamicProxyIgnoreTypes
{
    private static HashSet<Type> IgnoredTypes { get; } = new HashSet<Type>();

    /// <summary>
    /// 加入一个要忽略的类型
    /// </summary>
    /// <typeparam name="T">要忽略的类型</typeparam>
    public static void Add<T>()
    {
        Add(typeof(T));
    }

    /// <summary>
    /// 加入一个要忽略的类型
    /// </summary>
    /// <param name="type">要忽略的类型</param>
    public static void Add(Type type)
    {
        lock (IgnoredTypes)
        {
            IgnoredTypes.Add(type);
        }
    }

    /// <summary>
    /// 批量加入要忽略的类型
    /// </summary>
    /// <param name="types">要忽略的类型</param>
    public static void Add(params Type[] types)
    {
        lock (IgnoredTypes)
        {
            foreach (Type type in types)
            {
                IgnoredTypes.Add(type);
            }
        }
    }

    /// <summary>
    /// 判断类型是否在忽略清单里
    /// </summary>
    /// <param name="type">待判断的类型</param>
    /// <param name="includeDerivedTypes">是否把派生类型也算命中</param>
    /// <returns>在清单里时返回 <see langword="true"/></returns>
    public static bool Contains(Type type, bool includeDerivedTypes = true)
    {
        lock (IgnoredTypes)
        {
            return includeDerivedTypes
                ? IgnoredTypes.Any(ignored => ignored.IsAssignableFrom(type))
                : IgnoredTypes.Contains(type);
        }
    }
}
