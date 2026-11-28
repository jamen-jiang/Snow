using System;
using System.Linq;
using System.Reflection;

namespace Snow.DynamicProxy;

/// <summary>
/// 代理辅助方法: 判断对象是否为 Castle 生成的代理, 以及取回代理背后的真实对象。
/// </summary>
public static class SnowProxyHelper
{
    private const string ProxyNamespace = "Castle.Proxies";

    /// <summary>
    /// 判断给定对象是否为动态代理
    /// </summary>
    /// <param name="obj">待判断的对象</param>
    /// <returns>是代理时返回 <see langword="true"/></returns>
    public static bool IsProxy(object obj)
    {
        return obj.GetType().Namespace == ProxyNamespace;
    }

    /// <summary>
    /// 取回代理背后的真实对象; 不是代理时原样返回
    /// </summary>
    /// <param name="obj">代理对象或普通对象</param>
    /// <returns>真实对象</returns>
    public static object UnProxy(object obj)
    {
        if (obj.GetType().Namespace != ProxyNamespace)
        {
            return obj;
        }

        FieldInfo? targetField = obj.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(field => field.Name == "__target");

        if (targetField == null)
        {
            return obj;
        }

        return targetField.GetValue(obj)!;
    }

    /// <summary>
    /// 取对象的真实类型: 代理对象取被代理对象的类型
    /// </summary>
    /// <param name="obj">代理对象或普通对象</param>
    /// <returns>真实类型</returns>
    public static Type GetUnProxiedType(object obj)
    {
        if (obj.GetType().Namespace == ProxyNamespace)
        {
            object target = UnProxy(obj);
            if (target == obj)
            {
                return obj.GetType().GetTypeInfo().BaseType!;
            }

            return target.GetType();
        }

        return obj.GetType();
    }
}
