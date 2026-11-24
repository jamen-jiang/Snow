using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Snow.Aop;
using Snow.DependencyInjection;

namespace Snow.Aop.Castle;

/// <summary>
/// 基于 Castle DynamicProxy 的代理工厂。
/// <para>接口服务类型走接口代理(target 是真实实现实例), 因此实现类型可以是 sealed;
/// 服务类型是具体类时走类代理: 代理类型由 Castle 按实现类型的构造器自己造, 只有 virtual 方法会被拦截。</para>
/// <para>两条路都用同一份 <see cref="AopInterceptorAdapter"/>, 拦截器不需要知道自己包在哪种代理里。</para>
/// </summary>
[SingletonService]
public sealed class CastleAopProxyFactory : IAopProxyFactory
{
    private static readonly ProxyGenerator Generator = new();

    /// <summary>
    /// 已经通过类代理校验的实现类型, 避免每次解析都重新反射一遍
    /// </summary>
    private static readonly ConcurrentDictionary<Type, byte> ValidatedClassProxies = new();

    /// <inheritdoc />
    public object CreateProxy(IServiceProvider serviceProvider, Type implementationType, Type serviceType, SnowInterceptorAttribute[] interceptors)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(interceptors);

        AopInterceptorAdapter adapter = new(implementationType, interceptors, serviceProvider);

        if (serviceType.IsInterface)
        {
            object target = ActivatorUtilities.CreateInstance(serviceProvider, implementationType);

            return Generator.CreateInterfaceProxyWithTarget(serviceType, target, adapter);
        }

        EnsureClassProxyable(implementationType);

        return Generator.CreateClassProxy(
            implementationType,
            ProxyGenerationOptions.Default,
            ResolveConstructorArguments(serviceProvider, implementationType),
            adapter);
    }

    /// <summary>
    /// 类代理的三条硬约束, 违反时启动/首次解析就报错, 不静默放过
    /// </summary>
    /// <param name="implementationType">实现类型</param>
    /// <exception cref="ArgumentException">类型不能做类代理, 或标了拦截器的方法不是 virtual</exception>
    private static void EnsureClassProxyable(Type implementationType)
    {
        if (ValidatedClassProxies.ContainsKey(implementationType))
        {
            return;
        }

        if (implementationType.IsSealed || implementationType.IsAbstract)
        {
            throw new ArgumentException(
                "AOP 类代理要求具体类型: " + implementationType.FullName + " 是密封类或抽象类, 代理类型继承不了它。" +
                " 去掉拦截器注解, 或改成按接口解析(接口代理不要求实现类型可继承)。",
                nameof(implementationType));
        }

        foreach (MethodInfo method in implementationType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.DeclaringType == typeof(object) || method.IsDefined(typeof(SnowInterceptorAttribute), true) == false)
            {
                continue;
            }

            if (!method.IsVirtual || method.IsFinal)
            {
                throw new ArgumentException(
                    "AOP 类代理拦不到 " + implementationType.FullName + "." + method.Name +
                    ": 它标了拦截器但不是 virtual(或被 sealed override 掉了)。把方法改成 virtual, 或改成按接口解析。",
                    nameof(implementationType));
            }
        }

        ValidatedClassProxies.TryAdd(implementationType, 0);
    }

    /// <summary>
    /// 按实现类型的构造器从容器里解出实参: 参数最多的构造器优先, 解不出来的跳过, 可选参数取默认值
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="implementationType">实现类型</param>
    /// <returns>构造实参</returns>
    /// <exception cref="ArgumentException">没有哪个公开构造器的参数能全部解析出来</exception>
    private static object?[] ResolveConstructorArguments(IServiceProvider serviceProvider, Type implementationType)
    {
        ConstructorInfo[] constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        foreach (ConstructorInfo constructor in constructors.OrderByDescending(candidate => candidate.GetParameters().Length))
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            object?[] arguments = new object?[parameters.Length];
            bool resolved = true;

            for (int index = 0; index < parameters.Length; index++)
            {
                ParameterInfo parameter = parameters[index];
                object? value = serviceProvider.GetService(parameter.ParameterType);

                if (value is null && parameter.HasDefaultValue)
                {
                    value = parameter.DefaultValue;
                }
                else if (value is null)
                {
                    resolved = false;
                    break;
                }

                arguments[index] = value;
            }

            if (resolved)
            {
                return arguments;
            }
        }

        throw new ArgumentException(
            "AOP 类代理无法构造 " + implementationType.FullName + ": 没有哪个公开构造器的参数能全部从容器解析出来。",
            nameof(implementationType));
    }
}
