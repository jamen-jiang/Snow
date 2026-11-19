using Microsoft.Extensions.DependencyInjection;

namespace Rongia.DependencyInjection;

/// <summary>
/// 服务集合的对象访问扩展(对应 ABP 的 ServiceCollectionObjectAccessorExtensions):
/// 从尚未构建容器的服务集合里直接取单例实例
/// </summary>
public static class ServiceCollectionObjectAccessorExtensions
{
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
