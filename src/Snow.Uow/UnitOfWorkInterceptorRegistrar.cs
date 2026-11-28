using System.Reflection;
using Snow.DependencyInjection;
using Snow.DynamicProxy;

namespace Snow.Uow;

/// <summary>
/// 事务登记器(对应 ABP 的 <c>UnitOfWorkInterceptorRegistrar</c>): 实现类型(或它的方法/接口)带
/// <see cref="UnitOfWorkAttribute"/> 就挂上 <see cref="UnitOfWorkInterceptor"/>。
/// </summary>
public static class UnitOfWorkInterceptorRegistrar
{
    /// <summary>
    /// 按需登记
    /// </summary>
    /// <param name="context">服务登记上下文</param>
    public static void RegisterIfNeeded(IOnServiceRegistredContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (ShouldIntercept(context.ImplementationType))
        {
            context.Interceptors.TryAdd<UnitOfWorkInterceptor>();
        }
    }

    /// <summary>
    /// 判断实现类型是否需要挂事务拦截器(忽略清单里的类型直接跳过)
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>需要拦截时返回 <see langword="true"/></returns>
    private static bool ShouldIntercept(Type type)
    {
        if (SnowDynamicProxyIgnoreTypes.Contains(type))
        {
            return false;
        }

        return HasAttribute(type.GetCustomAttribute<UnitOfWorkAttribute>(true))
            || HasMethodAttribute(type);
    }

    /// <summary>
    /// 注解是否存在
    /// </summary>
    /// <param name="attribute">已读取的注解; 可为 <c>null</c></param>
    /// <returns>非 <c>null</c> 时返回 <see langword="true"/></returns>
    private static bool HasAttribute(UnitOfWorkAttribute? attribute) => attribute is not null;

    /// <summary>
    /// 判断类型自身或它实现的可见接口上是否有方法标了 <see cref="UnitOfWorkAttribute"/>
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>有方法级注解时返回 <see langword="true"/></returns>
    private static bool HasMethodAttribute(Type type)
    {
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (method.IsDefined(typeof(UnitOfWorkAttribute), true))
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
                if (method.IsDefined(typeof(UnitOfWorkAttribute), true))
                {
                    return true;
                }
            }
        }

        return false;
    }
}