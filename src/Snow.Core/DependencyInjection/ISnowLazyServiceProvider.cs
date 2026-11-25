namespace Snow.DependencyInjection;

/// <summary>
/// 惰性服务提供者契约(对应 ABP 的 <c>IAbpLazyServiceProvider</c>):
/// 解析结果缓存起来, 供属性注入的基类在使用时才取服务
/// </summary>
public interface ISnowLazyServiceProvider : ICachedServiceProviderBase
{
    /// <summary>
    /// 取服务, 解析不到时抛异常
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    T LazyGetRequiredService<T>();

    /// <summary>
    /// 取服务, 解析不到时抛异常
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns>服务实例</returns>
    object LazyGetRequiredService(Type serviceType);

    /// <summary>
    /// 取服务, 解析不到时返回 null
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例, 解析不到时为 null</returns>
    T? LazyGetService<T>();

    /// <summary>
    /// 取服务, 解析不到时返回 null
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns>服务实例, 解析不到时为 null</returns>
    object? LazyGetService(Type serviceType);

    /// <summary>
    /// 取服务, 解析不到时返回给定的默认值
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="defaultValue">解析不到时使用的默认值</param>
    /// <returns>服务实例或默认值</returns>
    T LazyGetService<T>(T defaultValue);

    /// <summary>
    /// 取服务, 解析不到时返回给定的默认值
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="defaultValue">解析不到时使用的默认值</param>
    /// <returns>服务实例或默认值</returns>
    object LazyGetService(Type serviceType, object defaultValue);

    /// <summary>
    /// 用工厂取服务, 工厂只在第一次调用时执行
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="factory">创建服务的工厂</param>
    /// <returns>服务实例</returns>
    object LazyGetService(Type serviceType, Func<IServiceProvider, object> factory);

    /// <summary>
    /// 用工厂取服务, 工厂只在第一次调用时执行
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="factory">创建服务的工厂</param>
    /// <returns>服务实例</returns>
    T LazyGetService<T>(Func<IServiceProvider, object> factory);
}
