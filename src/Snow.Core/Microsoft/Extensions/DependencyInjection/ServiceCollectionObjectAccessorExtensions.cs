using System;
using System.Linq;
using Snow.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务集合的对象访问器扩展(对应 ABP 的 <c>ServiceCollectionObjectAccessorExtensions</c>):
/// 登记/读取运行期填值的对象访问器。方法集与 ABP 完全一致。
/// </summary>
public static class ServiceCollectionObjectAccessorExtensions
{
    /// <summary>
    /// 已登记则直接返回既有的访问器, 否则新登记一个空访问器(对应 ABP 的 TryAddObjectAccessor)。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>对象访问器</returns>
    public static ObjectAccessor<T> TryAddObjectAccessor<T>(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>)))
        {
            return services.GetSingletonInstance<ObjectAccessor<T>>();
        }

        return services.AddObjectAccessor<T>();
    }

    /// <summary>
    /// 登记一个空的对象访问器(对应 ABP 的 AddObjectAccessor 无参重载)
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>对象访问器</returns>
    public static ObjectAccessor<T> AddObjectAccessor<T>(this IServiceCollection services)
    {
        return services.AddObjectAccessor(new ObjectAccessor<T>());
    }

    /// <summary>
    /// 用给定初始值登记一个对象访问器(对应 ABP 的 AddObjectAccessor(T obj) 重载)
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="obj">初始对象</param>
    /// <returns>对象访问器</returns>
    public static ObjectAccessor<T> AddObjectAccessor<T>(this IServiceCollection services, T obj)
    {
        return services.AddObjectAccessor(new ObjectAccessor<T>(obj));
    }

    /// <summary>
    /// 登记给定的对象访问器(对应 ABP 的核心重载)。
    /// <para>插到集合最前面以便快速取回; 同一类型重复登记会抛异常。</para>
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="accessor">对象访问器</param>
    /// <returns>对象访问器</returns>
    /// <exception cref="InvalidOperationException">该类型已登记过对象访问器</exception>
    public static ObjectAccessor<T> AddObjectAccessor<T>(this IServiceCollection services, ObjectAccessor<T> accessor)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(accessor);

        if (services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>)))
        {
            throw new InvalidOperationException("An object accessor is registered before for type: " + typeof(T).AssemblyQualifiedName);
        }

        // 放到最前面, 便于快速取回
        services.Insert(0, ServiceDescriptor.Singleton(typeof(ObjectAccessor<T>), accessor));
        services.Insert(0, ServiceDescriptor.Singleton(typeof(IObjectAccessor<T>), accessor));

        return accessor;
    }

    /// <summary>
    /// 从服务集合取对象访问器里当前的值, 没有则返回 null(对应 ABP 的 GetObjectOrNull)。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>对象实例或 null</returns>
    public static T? GetObjectOrNull<T>(this IServiceCollection services)
        where T : class
    {
        return services.GetSingletonInstanceOrNull<IObjectAccessor<T>>()?.Value;
    }

    /// <summary>
    /// 从服务集合取对象访问器里当前的值, 没有则抛异常(对应 ABP 的 GetObject)。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>对象实例</returns>
    /// <exception cref="InvalidOperationException">尚未用 AddObjectAccessor 登记该类型</exception>
    public static T GetObject<T>(this IServiceCollection services)
        where T : class
    {
        return services.GetObjectOrNull<T>()
            ?? throw new InvalidOperationException("Could not find an object of " + typeof(T).AssemblyQualifiedName + " in services. Be sure that you have used AddObjectAccessor before!");
    }
}
