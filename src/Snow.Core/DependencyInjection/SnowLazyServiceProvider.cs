using Microsoft.Extensions.DependencyInjection;

namespace Snow.DependencyInjection;

/// <summary>
/// 惰性服务提供者(对应 ABP 的 <c>AbpLazyServiceProvider</c>): 把取服务推迟到真正用的时候, 并缓存结果
/// </summary>
[TransientService]
[ExposeServices(typeof(ISnowLazyServiceProvider))]
public class SnowLazyServiceProvider : CachedServiceProviderBase, ISnowLazyServiceProvider
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">当前作用域的服务提供者</param>
    public SnowLazyServiceProvider(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <summary>
    /// 取服务, 解析不到时抛异常
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public virtual T LazyGetRequiredService<T>()
    {
        return (T)LazyGetRequiredService(typeof(T));
    }

    /// <summary>
    /// 取服务, 解析不到时抛异常
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns>服务实例</returns>
    public virtual object LazyGetRequiredService(Type serviceType)
    {
        return this.GetRequiredService(serviceType);
    }

    /// <summary>
    /// 取服务, 解析不到时返回 null
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例, 解析不到时为 null</returns>
    public virtual T? LazyGetService<T>()
    {
        return (T?)LazyGetService(typeof(T));
    }

    /// <summary>
    /// 取服务, 解析不到时返回 null
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns>服务实例, 解析不到时为 null</returns>
    public virtual object? LazyGetService(Type serviceType)
    {
        return GetService(serviceType);
    }

    /// <summary>
    /// 取服务, 解析不到时返回给定的默认值
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="defaultValue">解析不到时使用的默认值</param>
    /// <returns>服务实例或默认值</returns>
    public virtual T LazyGetService<T>(T defaultValue)
    {
        return GetService(defaultValue);
    }

    /// <summary>
    /// 取服务, 解析不到时返回给定的默认值
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="defaultValue">解析不到时使用的默认值</param>
    /// <returns>服务实例或默认值</returns>
    public virtual object LazyGetService(Type serviceType, object defaultValue)
    {
        return GetService(serviceType, defaultValue);
    }

    /// <summary>
    /// 用工厂取服务, 工厂只在第一次调用时执行
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="factory">创建服务的工厂</param>
    /// <returns>服务实例</returns>
    public virtual object LazyGetService(Type serviceType, Func<IServiceProvider, object> factory)
    {
        return GetService(serviceType, factory);
    }

    /// <summary>
    /// 用工厂取服务, 工厂只在第一次调用时执行
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="factory">创建服务的工厂</param>
    /// <returns>服务实例</returns>
    public virtual T LazyGetService<T>(Func<IServiceProvider, object> factory)
    {
        return GetService<T>(factory);
    }
}
