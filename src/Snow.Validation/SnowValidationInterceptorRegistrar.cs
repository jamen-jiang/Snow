using System.Reflection;
using Snow.DependencyInjection;
using Snow.DynamicProxy;

namespace Snow.Validation;

/// <summary>
/// 校验登记器: 实现类型(或它的方法/接口)带 <see cref="ValidationAttribute"/> 就挂上 <see cref="SnowValidationInterceptor"/>
/// </summary>
public static class SnowValidationInterceptorRegistrar
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
            context.Interceptors.TryAdd<SnowValidationInterceptor>();
        }
    }

    /// <summary>
    /// 判断实现类型是否需要挂校验拦截器(忽略清单里的类型直接跳过)
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>需要拦截时返回 <see langword="true"/></returns>
    private static bool ShouldIntercept(Type type)
    {
        if (SnowDynamicProxyIgnoreTypes.Contains(type))
        {
            return false;
        }

        if (type.IsDefined(typeof(ValidationAttribute), true))
        {
            return true;
        }

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (method.IsDefined(typeof(ValidationAttribute), true))
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
                if (method.IsDefined(typeof(ValidationAttribute), true))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
