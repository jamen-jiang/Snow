using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rongia.DependencyInjection;

/// <summary>
/// 约定注册入口(对应 ABP 的 AddConventionalRegistrar 与模块扫描注册)
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 登记自定义约定注册器;后续扫描会同时使用它和默认注册器
    /// </summary>
    /// <typeparam name="TRegistrar">注册器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddConventionalRegistrar<TRegistrar>(this IServiceCollection services)
        where TRegistrar : class, IConventionalRegistrar
    {
        services.TryAddSingleton<IConventionalRegistrar, TRegistrar>();
        return services;
    }

    /// <summary>
    /// 扫描给定程序集并按约定注册;容器内登记了自定义注册器时优先使用它们,否则使用默认注册器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">待扫描程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddConventionalRegistrars(this IServiceCollection services, params Assembly[] assemblies)
    {
        List<IConventionalRegistrar> registrars = ResolveRegistrars(services);

        foreach (IConventionalRegistrar registrar in registrars)
        {
            foreach (Assembly assembly in assemblies)
            {
                registrar.AddAssembly(services, assembly);
            }
        }

        return services;
    }

    /// <summary>
    /// 用指定注册器扫描程序集,不读取容器内已登记的自定义注册器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="registrar">注册器</param>
    /// <param name="assemblies">待扫描程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddConventionalRegistrars(this IServiceCollection services, IConventionalRegistrar registrar, params Assembly[] assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            registrar.AddAssembly(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// 注册单个类型,不扫描程序集
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="type">类型</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddConventionalType(this IServiceCollection services, Type type)
    {
        IConventionalRegistrar registrar = ResolveRegistrars(services)[0];
        registrar.AddTypes(services, type);
        return services;
    }

    /// <summary>
    /// 监听服务登记: 约定注册每往容器写入一个描述符都会回调一次。
    /// 用于需要"事后索引"的功能(例如事件总线收集处理器类型), 避免再扫一遍程序集
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="listener">监听器</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection OnRegistered(this IServiceCollection services,
        Action<OnServiceRegisteredContext> listener)
    {
        ServiceRegistrationListeners.Add(services, listener);
        return services;
    }

    private static List<IConventionalRegistrar> ResolveRegistrars(IServiceCollection services)
    {
        List<IConventionalRegistrar> registrars = new List<IConventionalRegistrar>();

        foreach (ServiceDescriptor descriptor in services.ToList())
        {
            if (descriptor.ServiceType != typeof(IConventionalRegistrar))
            {
                continue;
            }

            if (descriptor.ImplementationInstance is IConventionalRegistrar instance)
            {
                registrars.Add(instance);
                continue;
            }

            if (descriptor.ImplementationType != null)
            {
                if (Activator.CreateInstance(descriptor.ImplementationType) is IConventionalRegistrar created)
                {
                    registrars.Add(created);
                }
            }
        }

        if (registrars.Count == 0)
        {
            registrars.Add(new DefaultConventionalRegistrar());
        }

        return registrars;
    }
}
