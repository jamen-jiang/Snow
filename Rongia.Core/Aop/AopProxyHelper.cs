using System.Reflection;

namespace Rongia.Aop;

/// <summary>
/// 代理辅助方法: 判断对象是否为 Castle 动态代理, 并取回其真实目标对象。
/// </summary>
public static class AopProxyHelper
{
    private const string ProxyNamespace = "Castle.Proxies";

    /// <summary>
    /// 判断给定对象是否为动态代理
    /// </summary>
    /// <param name="obj">待判断的对象</param>
    /// <returns>是动态代理时返回 true</returns>
    public static bool IsProxy(object? obj)
    {
        return obj is not null && obj.GetType().Namespace == ProxyNamespace;
    }

    /// <summary>
    /// 取回被代理的真实目标对象; 非代理对象原样返回
    /// </summary>
    /// <param name="obj">代理对象或普通对象</param>
    /// <returns>真实目标对象</returns>
    public static object? UnProxy(object? obj)
    {
        if (!IsProxy(obj))
        {
            return obj;
        }

        FieldInfo? targetField = obj!.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(field => field.Name == "__target");

        return targetField?.GetValue(obj) ?? obj;
    }

    /// <summary>
    /// 取代理的类型; 是代理时返回真实目标的类型
    /// </summary>
    /// <param name="obj">代理对象或普通对象</param>
    /// <returns>未代理的类型</returns>
    public static Type GetUnProxiedType(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        object? target = UnProxy(obj);

        return target is null ? obj.GetType() : target.GetType();
    }
}
