using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rongia.Messaging;
using Rongia.Messaging.Entities;
using Rongia.Messaging.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 中介器的注册配置:要扫描的程序集、总线/发布者实现、管道行为、处理器与各类上限
/// </summary>
public class MessagingServiceConfiguration
{
    /// <summary>
    /// 类型注册过滤器,返回 true 的类型才会被注册;默认全部放行
    /// </summary>
    public Func<Type, bool> TypeEvaluator { get; set; } = t => true;
    
    /// <summary>
    /// 注册服务使用的生命周期,默认 <see cref="ServiceLifetime.Transient"/>
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// 由谁闭合"类型参数未一一对应"的开放式行为与处理器,
    /// 默认 <see cref="DependencyInjection.OpenBehaviorClosingStrategy.CloseForScannedRequests"/>
    /// </summary>
    public OpenBehaviorClosingStrategy OpenBehaviorClosingStrategy { get; set; }
        = OpenBehaviorClosingStrategy.CloseForScannedRequests;

    internal List<Assembly> AssembliesToRegister { get; } = new();

    /// <summary>
    /// 待注册的管道行为列表(按插入顺序决定管道顺序)
    /// </summary>
    public List<ServiceDescriptor> BehaviorsToRegister { get; } = new();

    /// <summary>
    /// 泛型请求处理器允许的类型参数个数上限;设为 0 表示不限制
    /// </summary>
    public int MaxGenericTypeParameters { get; set; } = 10;

    /// <summary>
    /// 用于闭合泛型请求类型参数约束的类型个数上限;设为 0 表示不限制
    /// </summary>
    public int MaxTypesClosing { get; set; } = 100;

    /// <summary>
    /// 允许尝试注册的泛型请求处理器数量上限;设为 0 表示不限制
    /// </summary>
    public int MaxGenericTypeRegistrations { get; set; } = 125000;

    /// <summary>
    /// 泛型处理器注册过程的超时时间(毫秒),超时即报错退出;设为 0 表示不限制
    /// </summary>
    public int RegistrationTimeout { get; set; } = 15000;

    /// <summary>
    /// 是否尝试注册自身带泛型类型参数的处理器
    /// </summary>
    public bool RegisterGenericHandlers { get; set; } = false;



    /// <summary>
    /// 从指定类型所在程序集扫描并注册处理器
    /// </summary>
    /// <typeparam name="T">用于定位程序集的类型</typeparam>
    /// <returns>当前配置对象,便于链式调用</returns>
    public MessagingServiceConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssemblyContaining(typeof(T));

    /// <summary>
    /// 从指定类型所在程序集扫描并注册处理器
    /// </summary>
    /// <param name="type">用于定位程序集的类型</param>
    /// <returns>当前配置对象,便于链式调用</returns>
    public MessagingServiceConfiguration RegisterServicesFromAssemblyContaining(Type type)
        => RegisterServicesFromAssembly(type.Assembly);

    /// <summary>
    /// 从指定程序集扫描并注册处理器
    /// </summary>
    /// <param name="assembly">待扫描的程序集</param>
    /// <returns>当前配置对象,便于链式调用</returns>
    public MessagingServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        AssembliesToRegister.Add(assembly);

        return this;
    }

    /// <summary>
    /// 从多个程序集扫描并注册处理器
    /// </summary>
    /// <param name="assemblies">待扫描的程序集</param>
    /// <returns>当前配置对象,便于链式调用</returns>
    public MessagingServiceConfiguration RegisterServicesFromAssemblies(
        params Assembly[] assemblies)
    {
        AssembliesToRegister.AddRange(assemblies);

        return this;
    }

    /// <summary>
    /// 注册一个闭式行为(显式指定服务接口与实现类型)
    /// </summary>
    /// <typeparam name="TServiceType">闭式行为接口类型</typeparam>
    /// <typeparam name="TImplementationType">闭式行为实现类型</typeparam>
    /// <param name="serviceLifetime">生命周期,默认 <see cref="ServiceLifetime.Transient"/></param>
    /// <returns>当前配置对象,便于链式调用</returns>
    public MessagingServiceConfiguration AddBehavior<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// 按实现类型实现的所有 <see cref="IPipelineBehavior{TRequest,TResponse}"/> 接口注册该闭式行为
    /// </summary>
    /// <typeparam name="TImplementationType">闭式行为实现类型</typeparam>
    /// <param name="serviceLifetime">生命周期,默认 <see cref="ServiceLifetime.Transient"/></param>
    /// <returns>当前配置对象,便于链式调用</returns>
    public MessagingServiceConfiguration AddBehavior<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return AddBehavior(typeof(TImplementationType), serviceLifetime);
    }

    /// <summary>
    /// 按实现类型实现的所有 <see cref="IPipelineBehavior{TRequest,TResponse}"/> 接口注册该闭式行为
    /// </summary>
    /// <param name="implementationType">闭式行为实现类型</param>
    /// <param name="serviceLifetime">生命周期,默认 <see cref="ServiceLifetime.Transient"/></param>
    /// <returns>当前配置对象,便于链式调用</returns>
    /// <exception cref="InvalidOperationException">该类型未实现 <see cref="IPipelineBehavior{TRequest,TResponse}"/></exception>
    public MessagingServiceConfiguration AddBehavior(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IPipelineBehavior<,>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
        }

        foreach (var implementedBehaviorType in implementedGenericInterfaces)
        {
            BehaviorsToRegister.Add(new ServiceDescriptor(implementedBehaviorType, implementationType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// 注册一个闭式行为(显式指定服务接口与实现类型)
    /// </summary>
    /// <param name="serviceType">闭式行为接口类型</param>
    /// <param name="implementationType">闭式行为实现类型</param>
    /// <param name="serviceLifetime">生命周期,默认 <see cref="ServiceLifetime.Transient"/></param>
    /// <returns>当前配置对象,便于链式调用</returns>
    public MessagingServiceConfiguration AddBehavior(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        BehaviorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// 按 <see cref="IPipelineBehavior{TRequest,TResponse}"/> 开放式泛型接口注册一个开放式泛型行为
    /// </summary>
    /// <param name="openBehaviorType">开放式泛型行为类型</param>
    /// <param name="serviceLifetime">生命周期,默认 <see cref="ServiceLifetime.Transient"/></param>
    /// <returns>当前配置对象,便于链式调用</returns>
    /// <exception cref="InvalidOperationException">该类型不是泛型,或未实现 <see cref="IPipelineBehavior{TRequest,TResponse}"/></exception>
    public MessagingServiceConfiguration AddOpenBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IPipelineBehavior<,>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            BehaviorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// 批量注册开放式泛型行为
    /// </summary>
    /// <param name="openBehaviorTypes">开放式泛型行为类型集合</param>
    /// <param name="serviceLifetime">生命周期,默认 <see cref="ServiceLifetime.Transient"/></param>
    /// <returns>当前配置对象,便于链式调用</returns>
    public MessagingServiceConfiguration AddOpenBehaviors(IEnumerable<Type> openBehaviorTypes, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        foreach (var openBehaviorType in openBehaviorTypes)
        {
            AddOpenBehavior(openBehaviorType, serviceLifetime);
        }

        return this;
    }

    /// <summary>
    /// 批量注册开放式泛型行为(带每个行为各自的生命周期)
    /// </summary>
    /// <param name="openBehaviors"><see cref="OpenBehavior"/> 条目集合</param>
    /// <returns>当前配置对象,便于链式调用</returns>
    public MessagingServiceConfiguration AddOpenBehaviors(IEnumerable<OpenBehavior> openBehaviors)
    {
        foreach (var openBehavior in openBehaviors)
        {
            AddOpenBehavior(openBehavior.OpenBehaviorType!, openBehavior.ServiceLifetime);
        }

        return this;
    }

}