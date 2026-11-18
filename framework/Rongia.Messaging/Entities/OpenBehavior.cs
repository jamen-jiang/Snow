using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Rongia.Messaging.Entities;

/// <summary>
/// 一个开放式泛型管道行为的注册条目(行为类型 + 生命周期)
/// </summary>
public class OpenBehavior
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="openBehaviorType">要注册的管道行为类型</param>
    /// <param name="serviceLifetime">注册的生命周期,默认 Transient</param>
    /// <exception cref="InvalidOperationException">该类型未实现 IPipelineBehavior</exception>
    /// <exception cref="ArgumentNullException"><paramref name="openBehaviorType"/> 为 null</exception>
    public OpenBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        ValidatePipelineBehaviorType(openBehaviorType);
        OpenBehaviorType = openBehaviorType;
        ServiceLifetime = serviceLifetime;
    }

    /// <summary>
    /// 开放式泛型行为类型
    /// </summary>
    public Type OpenBehaviorType { get; }

    /// <summary>
    /// 该行为的服务生命周期
    /// </summary>
    public ServiceLifetime ServiceLifetime { get; }

    /// <summary>
    /// 校验指定类型是否实现了 <see cref="IPipelineBehavior{TRequest, TResponse}"/>
    /// </summary>
    /// <param name="openBehaviorType">待校验的类型</param>
    /// <exception cref="InvalidOperationException">该类型未实现 <see cref="IPipelineBehavior{TRequest, TResponse}"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="openBehaviorType"/> 为 null</exception>
    private static void ValidatePipelineBehaviorType(Type openBehaviorType)
    {
        if (openBehaviorType == null) throw new ArgumentNullException($"Open behavior type can not be null.");

        bool isPipelineBehavior = openBehaviorType.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        if (!isPipelineBehavior)
        {
            throw new InvalidOperationException($"The type \"{openBehaviorType.Name}\" must implement IPipelineBehavior<,> interface.");
        }
    }
}
