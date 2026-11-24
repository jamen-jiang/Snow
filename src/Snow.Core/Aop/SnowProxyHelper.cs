using System.Collections.Concurrent;
using System.Reflection;

namespace Snow.Aop;

/// <summary>
/// 代理对象的辅助方法。
/// <para>接口代理的运行时类型是 Castle 生成的子类(命名空间 Castle.Proxies), 不继承实现类型, 按具体类型做 as 转换或用对象做字典键都会因此失效;
/// 类代理虽然继承实现类型, GetType() 的精确比较照样会看到代理类型。需要取回原始实例时用 <see cref="UnProxy{T}"/>。</para>
/// </summary>
public static class SnowProxyHelper
{
    /// <summary>
    /// Castle 生成的代理类型所在的命名空间
    /// </summary>
    private const string ProxyNamespace = "Castle.Proxies";

    /// <summary>
    /// 带目标对象创建的代理把目标实例放在这个字段里(类代理与接口代理同名)
    /// </summary>
    private const string TargetFieldName = "__target";

    /// <summary>
    /// 按代理类型缓存目标字段; 没有目标字段(如无目标的类代理)记为 <see langword="null"/>
    /// </summary>
    private static readonly ConcurrentDictionary<Type, FieldInfo?> TargetFields = new();

    /// <summary>
    /// 取回被代理的原始实例: 传入代理时返回它包着的目标对象, 传入普通对象时原样返回
    /// </summary>
    /// <typeparam name="T">静态类型</typeparam>
    /// <param name="instance">对象</param>
    /// <returns>接口代理返回它包着的实现类型实例; 类代理与普通对象自身就是唯一实例, 原样返回</returns>
    public static T UnProxy<T>(this T instance) where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);

        Type type = instance.GetType();

        if (type.Namespace != ProxyNamespace)
        {
            return instance;
        }

        FieldInfo? targetField = TargetFields.GetOrAdd(
            type,
            static proxyType => proxyType.GetField(TargetFieldName, BindingFlags.Instance | BindingFlags.NonPublic));

        return targetField?.GetValue(instance) as T ?? instance;
    }
}
