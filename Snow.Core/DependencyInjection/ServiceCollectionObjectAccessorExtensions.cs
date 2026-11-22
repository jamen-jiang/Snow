using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Snow.DependencyInjection;

/// <summary>
/// 服务集合的对象访问扩展(对应 ABP 的 ServiceCollectionObjectAccessorExtensions):
/// 从尚未构建容器的服务集合里直接取单例实例, 以及登记运行期填值的对象访问器
/// </summary>
public static class ServiceCollectionObjectAccessorExtensions
{
    /// <summary>
    /// 登记一个对象访问器(对应 ABP 的 AddObjectAccessor):
    /// 容器里既能解析 <see cref="ObjectAccessor{T}"/>, 也能解析 <see cref="IObjectAccessor{T}"/>, 两者是同一个实例
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="obj">初始对象, 可空, 由宿主在容器构建完成后填值</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddObjectAccessor<T>(this IServiceCollection services, T? obj = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(services);

        ObjectAccessor<T> accessor = new() { Value = obj };

        services.TryAddSingleton(accessor);
        services.TryAddSingleton<IObjectAccessor<T>>(accessor);

        return services;
    }

    /// <summary>
    /// 取以实例方式注册的单例,不存在时返回 null。
    /// 只认 ImplementationInstance,不会为了取一个对象去构建临时容器
    /// (构建容器会让单例提前实例化,也会与宿主自己的容器重复)。
    /// 因此宿主必须在模块装配前把 IConfiguration 这类共享对象以实例形式注册进来。
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>单例实例或 null</returns>
    public static T? GetSingletonInstanceOrNull<T>(this IServiceCollection services)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(services);

        ServiceDescriptor? descriptor = services.FirstOrDefault(service =>
            service.ServiceType == typeof(T) && service.ImplementationInstance != null);

        return descriptor?.ImplementationInstance as T;
    }

    /// <summary>
    /// 取以实例方式注册的单例,不存在时抛异常
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>单例实例</returns>
    /// <exception cref="InvalidOperationException">服务集合中没有该类型的单例实例</exception>
    public static T GetSingletonInstance<T>(this IServiceCollection services)
        where T : class
    {
        return services.GetSingletonInstanceOrNull<T>()
            ?? throw new InvalidOperationException(
                "服务集合中找不到以实例方式注册的单例: " + typeof(T).FullName + ",宿主需在模块装配前把它注册进来");
    }
}
