using System.Reflection;
using Snow.DependencyInjection;
using Snow.DynamicProxy;

namespace Snow.Caching;

/// <summary>
/// 缓存登记器: 实现类型(或它的方法/接口)带 <see cref="CacheableAttribute"/> 挂 <see cref="CacheableInterceptor"/>,
/// 带 <see cref="CacheEvictAttribute"/> 挂 <see cref="CacheEvictInterceptor"/>
/// </summary>
public static class SnowCachingInterceptorRegistrar
{
    /// <summary>
    /// 按需登记
    /// </summary>
    /// <param name="context">服务登记上下文</param>
    public static void RegisterIfNeeded(IOnServiceRegistredContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (SnowDynamicProxyIgnoreTypes.Contains(context.ImplementationType))
        {
            return;
        }

        if (HasAttribute<CacheableAttribute>(context.ImplementationType))
        {
            context.Interceptors.TryAdd<CacheableInterceptor>();
        }

        if (HasAttribute<CacheEvictAttribute>(context.ImplementationType))
        {
            context.Interceptors.TryAdd<CacheEvictInterceptor>();
        }
    }

    /// <summary>
    /// 判断类型自身或它实现的可见接口上是否有方法标了指定注解
    /// </summary>
    /// <typeparam name="TAttribute">注解类型</typeparam>
    /// <param name="type">实现类型</param>
    /// <returns>有方法级注解时返回 <see langword="true"/></returns>
    private static bool HasAttribute<TAttribute>(Type type) where TAttribute : Attribute
    {
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (method.IsDefined(typeof(TAttribute), true))
            {
                return true;
            }
        }

        foreach (Type contract in type.GetInterfaces())
        {
            if (!contract.IsVisible)
            {
                continue;
            }

            foreach (MethodInfo method in contract.GetMethods())
            {
                if (method.IsDefined(typeof(TAttribute), true))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
