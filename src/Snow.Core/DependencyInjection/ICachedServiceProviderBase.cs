using Microsoft.Extensions.DependencyInjection;

namespace Snow.DependencyInjection;

/// <summary>
/// 缓存解析结果的服务提供者契约基接口(对应 ABP 的 <c>ICachedServiceProviderBase</c>):
/// 同一个服务类型只向容器要一次, 之后都返回缓存下来的实例, 瞬态服务也一样
/// </summary>
public interface ICachedServiceProviderBase : IKeyedServiceProvider
{
    /// <summary>
    /// 取服务, 解析不到时返回给定的默认值
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="defaultValue">解析不到时使用的默认值</param>
    /// <returns>服务实例或默认值</returns>
    T GetService<T>(T defaultValue);

    /// <summary>
    /// 取服务, 解析不到时返回给定的默认值
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="defaultValue">解析不到时使用的默认值</param>
    /// <returns>服务实例或默认值</returns>
    object GetService(Type serviceType, object defaultValue);

    /// <summary>
    /// 用工厂取服务, 工厂只在第一次调用时执行
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="factory">创建服务的工厂</param>
    /// <returns>服务实例</returns>
    T GetService<T>(Func<IServiceProvider, object> factory);

    /// <summary>
    /// 用工厂取服务, 工厂只在第一次调用时执行
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="factory">创建服务的工厂</param>
    /// <returns>服务实例</returns>
    object GetService(Type serviceType, Func<IServiceProvider, object> factory);
}
