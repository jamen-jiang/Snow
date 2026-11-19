using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Rongia.Aop;

namespace Rongia.Aop.Castle;

/// <summary>
/// 基于 Castle DynamicProxy 的代理工厂: 以接口代理(target 为真实实现实例)的方式创建代理,
/// 因此目标类型可以是 sealed, 但只能通过接口服务类型解析。
/// </summary>
public sealed class CastleAopProxyFactory : IAopProxyFactory
{
    private static readonly ProxyGenerator Generator = new();

    /// <inheritdoc />
    public object CreateProxy(IServiceProvider serviceProvider, Type implementationType, Type serviceType, InterceptorAttribute[] interceptors)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(interceptors);

        if (!serviceType.IsInterface)
        {
            throw new ArgumentException("AOP 代理只支持接口服务类型: " + serviceType.FullName, nameof(serviceType));
        }

        object target = ActivatorUtilities.CreateInstance(serviceProvider, implementationType);

        return Generator.CreateInterfaceProxyWithTarget(
            serviceType,
            target,
            new AopInterceptorAdapter(implementationType, interceptors, serviceProvider));
    }
}
