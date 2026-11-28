using System.Reflection;
using Snow.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 约定登记器扩展(对应 ABP 的 <c>ServiceCollectionConventionalRegistrationExtensions</c>):
/// <see cref="AddConventionalRegistrar"/> 登记一个登记器; <see cref="AddType"/> / <see cref="AddTypes"/> / <see cref="AddAssembly"/>
/// 把手工发现的类型交给全部登记器, 最终并入约定注册候选。
/// </summary>
public static class ServiceCollectionConventionalRegistrationExtensions
{
    /// <summary>
    /// 登记一个约定登记器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="registrar">登记器</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddConventionalRegistrar(this IServiceCollection services, IConventionalRegistrar registrar)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registrar);

        GetOrCreateRegistrarList(services).Add(registrar);

        return services;
    }

    /// <summary>
    /// 读取已登记的约定登记器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>登记器清单</returns>
    public static IReadOnlyList<IConventionalRegistrar> GetConventionalRegistrars(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return GetOrCreateRegistrarList(services);
    }

    /// <summary>
    /// 把程序集交给全部登记器扫描
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assembly">待扫描程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAssembly(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (IConventionalRegistrar registrar in GetOrCreateRegistrarList(services))
        {
            registrar.AddAssembly(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// 把某个类型所在的程序集交给全部登记器扫描
    /// </summary>
    /// <typeparam name="T">程序集里的任意类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAssemblyOf<T>(this IServiceCollection services)
    {
        return services.AddAssembly(typeof(T).Assembly);
    }

    /// <summary>
    /// 把一批类型交给全部登记器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="types">类型数组</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTypes(this IServiceCollection services, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(types);

        foreach (IConventionalRegistrar registrar in GetOrCreateRegistrarList(services))
        {
            registrar.AddTypes(services, types);
        }

        return services;
    }

    /// <summary>
    /// 把单个类型交给全部登记器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="type">实现类型</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddType(this IServiceCollection services, Type type)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(type);

        foreach (IConventionalRegistrar registrar in GetOrCreateRegistrarList(services))
        {
            registrar.AddType(services, type);
        }

        return services;
    }

    /// <summary>
    /// 尝试把单个类型交给全部登记器, 并报告是否有登记器接管了它
    /// <para>调用方(如消息模块的处理器扫描)据此决定是否退回直接登记, 避免同一条服务既走约定注册又被裸登记。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="type">实现类型</param>
    /// <returns>有登记器接管时返回 <see langword="true"/></returns>
    public static bool TryAddConventionalType(this IServiceCollection services, Type type)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(type);

        foreach (IConventionalRegistrar registrar in GetOrCreateRegistrarList(services))
        {
            registrar.AddType(services, type);
        }

        foreach (ExternalConventionalRegistration registration in GetOrCreateExternalList(services))
        {
            if (registration.ImplementationType == type)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 记一条外部约定候选(同实现类型只记一次)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="registration">候选</param>
    internal static void AddExternalConventionalRegistration(this IServiceCollection services, ExternalConventionalRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(services);

        ExternalConventionalRegistrationList list = GetOrCreateExternalList(services);

        foreach (ExternalConventionalRegistration existing in list)
        {
            if (existing.ImplementationType == registration.ImplementationType)
            {
                return;
            }
        }

        list.Add(registration);
    }

    /// <summary>
    /// 读取外部约定候选
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>候选清单</returns>
    internal static IReadOnlyList<ExternalConventionalRegistration> GetExternalConventionalRegistrations(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return GetOrCreateExternalList(services);
    }

    /// <summary>
    /// 取登记器清单单例; 没有就新建并登记
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>登记器清单</returns>
    private static ConventionalRegistrarList GetOrCreateRegistrarList(IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(ConventionalRegistrarList) &&
                descriptor.ImplementationInstance is ConventionalRegistrarList existing)
            {
                return existing;
            }
        }

        ConventionalRegistrarList list = new();
        services.AddSingleton(list);

        return list;
    }

    /// <summary>
    /// 取外部候选清单单例; 没有就新建并登记
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>候选清单</returns>
    private static ExternalConventionalRegistrationList GetOrCreateExternalList(IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(ExternalConventionalRegistrationList) &&
                descriptor.ImplementationInstance is ExternalConventionalRegistrationList existing)
            {
                return existing;
            }
        }

        ExternalConventionalRegistrationList list = new();
        services.AddSingleton(list);

        return list;
    }
}
