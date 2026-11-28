using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Snow.Castle.DynamicProxy;
using Snow.DependencyInjection;
using Snow.DynamicProxy;

namespace Snow.Castle;

/// <summary>
/// 基于 Castle DynamicProxy 的代理工厂。
/// <para>接口服务类型走接口代理(target 是真实实现实例), 因此实现类型可以是 sealed;
/// 代理会一并实现 target 的 IDisposable/IAsyncDisposable 并转发, 保证容器能释放 target。</para>
/// <para>服务类型是具体类时走类代理, 只有 virtual 方法会被拦截。</para>
/// </summary>
[SingletonService]
public class CastleSnowProxyFactory : ISnowProxyFactory
{
    private static readonly ProxyGenerator Generator = new();
    private static readonly ConcurrentDictionary<Type, byte> ValidatedClassProxies = new();

    /// <inheritdoc />
    public object CreateProxy(
        IServiceProvider serviceProvider,
        Type implementationType,
        Type serviceType,
        IReadOnlyList<Type> interceptorTypes)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(interceptorTypes);

        IInterceptor[] interceptors = ResolveInterceptors(serviceProvider, interceptorTypes);

        if (serviceType.IsInterface)
        {
            object target = ActivatorUtilities.CreateInstance(serviceProvider, implementationType);

            // target 是手工创建的, 容器原本不认识它; 让接口代理同时实现 target 的释放接口,
            // 容器释放代理时就会沿 Castle 的转发释放 target。
            return Generator.CreateInterfaceProxyWithTarget(
                serviceType,
                GetAdditionalInterfaces(implementationType, serviceType),
                target,
                interceptors);
        }

        EnsureClassProxyable(implementationType);

        return Generator.CreateClassProxy(
            implementationType,
            ProxyGenerationOptions.Default,
            ResolveConstructorArguments(serviceProvider, implementationType),
            interceptors);
    }

    private static Type[] GetAdditionalInterfaces(Type implementationType, Type serviceType)
    {
        List<Type> additionalInterfaces = new();

        if (typeof(IDisposable).IsAssignableFrom(implementationType) && serviceType != typeof(IDisposable))
        {
            additionalInterfaces.Add(typeof(IDisposable));
        }

        if (typeof(IAsyncDisposable).IsAssignableFrom(implementationType) && serviceType != typeof(IAsyncDisposable))
        {
            additionalInterfaces.Add(typeof(IAsyncDisposable));
        }

        return additionalInterfaces.ToArray();
    }

    private static IInterceptor[] ResolveInterceptors(IServiceProvider serviceProvider, IReadOnlyList<Type> interceptorTypes)
    {
        IInterceptor[] interceptors = new IInterceptor[interceptorTypes.Count];

        for (int index = 0; index < interceptorTypes.Count; index++)
        {
            Type adapterType = typeof(SnowAsyncDeterminationInterceptor<>).MakeGenericType(interceptorTypes[index]);
            interceptors[index] = (IInterceptor)serviceProvider.GetRequiredService(adapterType);
        }

        return interceptors;
    }

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
                " 去掉拦截器, 或改成按接口解析(接口代理不要求实现类型可继承)。",
                nameof(implementationType));
        }

        ValidatedClassProxies.TryAdd(implementationType, 0);
    }

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
