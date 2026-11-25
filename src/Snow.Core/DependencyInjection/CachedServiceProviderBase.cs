using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.DependencyInjection;

/// <summary>
/// 缓存解析结果的服务提供者基类(对应 ABP 的 <c>CachedServiceProviderBase</c>):
/// 每个服务标识只解析一次, 结果放进 <see cref="CachedServices"/>
/// <para>缓存键是"服务类型 + 键"的组合, 因此键控服务也能各自缓存; <see cref="IServiceProvider"/> 自身预置在缓存里,
/// 需要容器的代码不必再走一次解析。</para>
/// </summary>
public abstract class CachedServiceProviderBase : ICachedServiceProviderBase
{
    /// <summary>
    /// 真正的服务提供者, 缓存未命中时向它要实例
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 解析结果缓存: 值是 <see cref="Lazy{T}"/> 包一层, 保证同一个服务标识只解析一次
    /// </summary>
    protected ConcurrentDictionary<ServiceIdentifier, Lazy<object?>> CachedServices { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">真正的服务提供者</param>
    protected CachedServiceProviderBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        CachedServices = new ConcurrentDictionary<ServiceIdentifier, Lazy<object?>>();
        CachedServices.TryAdd(new ServiceIdentifier(typeof(IServiceProvider)), new Lazy<object?>(() => ServiceProvider));
    }

    /// <summary>
    /// 取服务, 解析不到时返回 null
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns>服务实例, 解析不到时为 null</returns>
    public virtual object? GetService(Type serviceType)
    {
        return CachedServices.GetOrAdd(
            new ServiceIdentifier(serviceType),
            _ => new Lazy<object?>(() => ServiceProvider.GetService(serviceType))
        ).Value;
    }

    /// <summary>
    /// 取服务, 解析不到时返回给定的默认值
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="defaultValue">解析不到时使用的默认值</param>
    /// <returns>服务实例或默认值</returns>
    public T GetService<T>(T defaultValue)
    {
        return (T)GetService(typeof(T), defaultValue!);
    }

    /// <summary>
    /// 取服务, 解析不到时返回给定的默认值
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="defaultValue">解析不到时使用的默认值</param>
    /// <returns>服务实例或默认值</returns>
    public object GetService(Type serviceType, object defaultValue)
    {
        return GetService(serviceType) ?? defaultValue;
    }

    /// <summary>
    /// 用工厂取服务, 工厂只在第一次调用时执行
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="factory">创建服务的工厂</param>
    /// <returns>服务实例</returns>
    public T GetService<T>(Func<IServiceProvider, object> factory)
    {
        return (T)GetService(typeof(T), factory);
    }

    /// <summary>
    /// 用工厂取服务, 工厂只在第一次调用时执行
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="factory">创建服务的工厂</param>
    /// <returns>服务实例</returns>
    public object GetService(Type serviceType, Func<IServiceProvider, object> factory)
    {
        return CachedServices.GetOrAdd(
            new ServiceIdentifier(serviceType),
            _ => new Lazy<object?>(() => factory(ServiceProvider))
        ).Value!;
    }

    /// <summary>
    /// 按键取服务, 解析不到时返回 null
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="serviceKey">服务键</param>
    /// <returns>服务实例, 解析不到时为 null</returns>
    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        return CachedServices.GetOrAdd(
            new ServiceIdentifier(serviceKey, serviceType),
            _ => new Lazy<object?>(() => ServiceProvider.GetKeyedService(serviceType, serviceKey))
        ).Value;
    }

    /// <summary>
    /// 按键取服务, 解析不到时抛异常
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="serviceKey">服务键</param>
    /// <returns>服务实例</returns>
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        return CachedServices.GetOrAdd(
            new ServiceIdentifier(serviceKey, serviceType),
            _ => new Lazy<object?>(() => ServiceProvider.GetRequiredKeyedService(serviceType, serviceKey))
        ).Value!;
    }
}
