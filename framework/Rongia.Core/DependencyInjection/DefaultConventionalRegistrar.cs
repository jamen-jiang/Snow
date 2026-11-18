using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.DependencyInjection;

/// <summary>
/// 默认约定注册器:按标记接口或 DependencyAttribute 决定生命周期,
/// 按 ExposeServicesAttribute 或默认规则决定暴露的服务类型
/// </summary>
public class DefaultConventionalRegistrar : ConventionalRegistrarBase
{
    /// <summary>
    /// 注册单个类型;既未实现标记接口又未标注 DependencyAttribute 的类型会被跳过
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="type">类型</param>
    public override void AddType(IServiceCollection services, Type type)
    {
        DependencyAttribute? dependency = type.GetCustomAttribute<DependencyAttribute>(true);

        ServiceLifetime? lifetime = dependency?.Lifetime ?? GetLifetimeFromMarker(type);
        if (lifetime == null)
        {
            return;
        }

        Type[] exposedServices = ExposedServiceExplorer.GetExposedServices(type);

        foreach (Type serviceType in exposedServices)
        {
            if (IsMarkerInterface(serviceType))
            {
                continue;
            }

            ServiceDescriptor descriptor = ServiceDescriptor.Describe(serviceType, type, lifetime.Value);

            if (dependency != null && dependency.ReplaceServices)
            {
                ReplaceService(services, descriptor);
            }
            else
            {
                AddIfNotExists(services, descriptor);
            }

            ServiceRegistrationListeners.Notify(services, descriptor);
        }
    }

    /// <summary>
    /// 按标记接口推断生命周期
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>生命周期;未标记时返回 null</returns>
    protected virtual ServiceLifetime? GetLifetimeFromMarker(Type type)
    {
        if (typeof(ISingletonDependency).IsAssignableFrom(type))
        {
            return ServiceLifetime.Singleton;
        }

        if (typeof(IScopedDependency).IsAssignableFrom(type))
        {
            return ServiceLifetime.Scoped;
        }

        if (typeof(ITransientDependency).IsAssignableFrom(type))
        {
            return ServiceLifetime.Transient;
        }

        return null;
    }

    private static bool IsMarkerInterface(Type serviceType)
    {
        return serviceType == typeof(ITransientDependency)
            || serviceType == typeof(IScopedDependency)
            || serviceType == typeof(ISingletonDependency);
    }

    private static void AddIfNotExists(IServiceCollection services, ServiceDescriptor descriptor)
    {
        foreach (ServiceDescriptor existing in services)
        {
            if (existing.ServiceType != descriptor.ServiceType)
            {
                continue;
            }

            if (existing.ImplementationType == descriptor.ImplementationType
                && existing.ImplementationInstance == descriptor.ImplementationInstance)
            {
                return;
            }
        }

        services.Add(descriptor);
    }

    /// <summary>
    /// 覆盖注册:只清除与本次描述符服务类型和实现方式都相同的既有登记,再追加新描述符。
    /// 保留实现方式判据是有意为之 —— 同一个服务类型的其他实现必须原样留存,
    /// 否则一个实现标记 ReplaceServices 会把走同一接口的兄弟实现一起清掉,
    /// 导致 GetServices 少返回、按实现类型解析失败
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="descriptor">待登记的描述符</param>
    private static void ReplaceService(IServiceCollection services, ServiceDescriptor descriptor)
    {
        for (int index = services.Count - 1; index >= 0; index--)
        {
            ServiceDescriptor existing = services[index];

            if (existing.ServiceType != descriptor.ServiceType)
            {
                continue;
            }

            if (existing.ImplementationType != descriptor.ImplementationType
                || !ReferenceEquals(existing.ImplementationInstance, descriptor.ImplementationInstance))
            {
                continue;
            }

            services.RemoveAt(index);
        }

        services.Add(descriptor);
    }
}
