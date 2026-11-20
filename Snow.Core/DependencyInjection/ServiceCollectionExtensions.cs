using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Snow.Aop;

namespace Snow.DependencyInjection;

/// <summary>
/// 约定注册入口:每个程序集只枚举一次类型,收集完候选后按生命周期分桶登记 ——
/// 生命周期来自标记接口或 <see cref="DependencyAttribute"/>,暴露的服务类型来自
/// <see cref="ExposeServicesAttribute"/> 或默认推断(<see cref="ExposedServiceExplorer"/>)。
/// <para>同一批程序集只做一趟扫描,不再按生命周期重复枚举。</para>
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly ServiceLifetime[] AllLifetimes =
    {
        ServiceLifetime.Singleton,
        ServiceLifetime.Scoped,
        ServiceLifetime.Transient,
    };

    /// <summary>
    /// 扫描给定程序集并按约定注册
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">待扫描程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddConventionalRegistrars(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assemblies.Length == 0)
        {
            return services;
        }

        List<ConventionalRegistration> candidates = CollectCandidates(assemblies);

        // 按生命周期分桶追加,保持"单例 → 作用域 → 瞬态"的登记顺序
        foreach (ServiceLifetime lifetime in AllLifetimes)
        {
            foreach (ConventionalRegistration candidate in candidates)
            {
                if (candidate.Lifetime == lifetime && !candidate.ReplaceServices)
                {
                    AddDescriptors(services, candidate);
                }
            }
        }

        // 替换语义最后处理:清掉同服务类型的既有登记再重建
        foreach (ConventionalRegistration candidate in candidates)
        {
            if (!candidate.ReplaceServices)
            {
                continue;
            }

            foreach (Type serviceType in candidate.ServiceTypes)
            {
                RemoveDescriptors(services, serviceType);
            }

            AddDescriptors(services, candidate);
        }

        return services;
    }

    /// <summary>
    /// 一趟扫描收集全部候选类型及其生命周期与暴露的服务类型
    /// </summary>
    /// <param name="assemblies">待扫描程序集</param>
    /// <returns>候选登记列表,顺序与程序集、类型声明顺序一致</returns>
    private static List<ConventionalRegistration> CollectCandidates(Assembly[] assemblies)
    {
        List<ConventionalRegistration> candidates = new();
        HashSet<Assembly> scannedAssemblies = new();

        foreach (Assembly assembly in assemblies)
        {
            if (!scannedAssemblies.Add(assembly))
            {
                continue;
            }

            foreach (Type type in AssemblyTypeCache.GetTypes(assembly))
            {
                if (!type.IsClass || type.IsAbstract || !type.IsVisible)
                {
                    continue;
                }

                if (type.IsDefined(typeof(DisableConventionalRegistrationAttribute), true))
                {
                    continue;
                }

                ServiceLifetime? lifetime = GetLifetime(type);
                InterceptorAttribute[] interceptors = GetInterceptors(type);
                bool hasInterceptors = interceptors.Length > 0 || HasMethodInterceptors(type);

                if (lifetime is null)
                {
                    // 只打拦截器注解、未标生命周期时默认按瞬态登记, 做到"一个注解即可用"
                    if (!hasInterceptors)
                    {
                        continue;
                    }

                    lifetime = ServiceLifetime.Transient;
                }

                Type[] serviceTypes = GetExposedServiceTypes(type).ToArray();

                if (serviceTypes.Length == 0)
                {
                    continue;
                }

                candidates.Add(new ConventionalRegistration(type, lifetime.Value, serviceTypes, IsReplacing(type), interceptors, hasInterceptors));
            }
        }

        return candidates;
    }

    /// <summary>
    /// 按服务类型追加描述符
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="candidate">候选登记</param>
    private static void AddDescriptors(IServiceCollection services, ConventionalRegistration candidate)
    {
        foreach (Type serviceType in candidate.ServiceTypes)
        {
            if (!candidate.HasInterceptors)
            {
                services.Add(new ServiceDescriptor(serviceType, candidate.ImplementationType, candidate.Lifetime));
                continue;
            }

            // 代理只覆盖接口服务类型; 把实现类自身当作服务类型登记会拿到未被代理的实例, 因此跳过
            if (!serviceType.IsInterface)
            {
                continue;
            }

            Type implementationType = candidate.ImplementationType;
            InterceptorAttribute[] interceptors = candidate.Interceptors;

            services.Add(new ServiceDescriptor(
                serviceType,
                serviceProvider => CreateProxy(serviceProvider, implementationType, serviceType, interceptors),
                candidate.Lifetime));
        }
    }

    /// <summary>
    /// 读取类型上的类级拦截器实例(特性即拦截器, 实例由反射创建)
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>类级拦截器; 未声明时返回空数组</returns>
    private static InterceptorAttribute[] GetInterceptors(Type type)
    {
        return type.GetCustomAttributes<InterceptorAttribute>(true).ToArray();
    }

    /// <summary>
    /// 判断类型的 public 实现方法上是否标注了拦截器
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>标注了返回 <see langword="true"/></returns>
    private static bool HasMethodInterceptors(Type type)
    {
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.IsDefined(typeof(InterceptorAttribute), true))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 通过容器里的代理工厂创建代理; 未启用 AOP 时给出明确错误
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="implementationType">实现类型</param>
    /// <param name="serviceType">接口服务类型</param>
    /// <param name="interceptors">类级拦截器</param>
    /// <returns>代理对象</returns>
    /// <exception cref="InvalidOperationException">容器中缺少 <see cref="IAopProxyFactory"/></exception>
    private static object CreateProxy(IServiceProvider serviceProvider, Type implementationType, Type serviceType, InterceptorAttribute[] interceptors)
    {
        IAopProxyFactory? factory = serviceProvider.GetService<IAopProxyFactory>();

        if (factory is null)
        {
            throw new InvalidOperationException(
                "类型 " + implementationType.FullName + " 标注了 [" + nameof(InterceptorAttribute) +
                "], 但容器中没有 " + nameof(IAopProxyFactory) + "; 请让宿主模块依赖 SnowAopModule(Snow.Aop.Castle 包)以启用 AOP。");
        }

        return factory.CreateProxy(serviceProvider, implementationType, serviceType, interceptors);
    }

    /// <summary>
    /// 移除某个服务类型的全部既有登记
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="serviceType">服务类型</param>
    private static void RemoveDescriptors(IServiceCollection services, Type serviceType)
    {
        for (int index = services.Count - 1; index >= 0; index--)
        {
            if (services[index].ServiceType == serviceType)
            {
                services.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// 生命周期判定:<see cref="DependencyAttribute"/> 显式指定优先,否则按标记接口推断
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>生命周期;既无标记也无特性时返回 null</returns>
    private static ServiceLifetime? GetLifetime(Type type)
    {
        DependencyAttribute? dependency = type.GetCustomAttribute<DependencyAttribute>(true);

        if (dependency?.Lifetime is not null)
        {
            return dependency.Lifetime;
        }

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

    /// <summary>
    /// 是否要求替换同服务类型的既有登记
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>需要替换时返回 true</returns>
    private static bool IsReplacing(Type type)
    {
        return type.GetCustomAttribute<DependencyAttribute>(true)?.ReplaceServices == true;
    }

    /// <summary>
    /// 暴露的服务类型:去掉三个生命周期标记本身,其余由 <see cref="ExposedServiceExplorer"/> 决定
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>服务类型集合</returns>
    private static IEnumerable<Type> GetExposedServiceTypes(Type type)
    {
        return ExposedServiceExplorer.GetExposedServices(type).Where(serviceType => !IsMarkerInterface(serviceType));
    }

    /// <summary>
    /// 是否为生命周期标记接口
    /// </summary>
    /// <param name="type">接口类型</param>
    /// <returns>是标记接口时返回 true</returns>
    private static bool IsMarkerInterface(Type type)
    {
        return type == typeof(ITransientDependency)
            || type == typeof(IScopedDependency)
            || type == typeof(ISingletonDependency);
    }

    /// <summary>
    /// 候选登记:实现类型、生命周期、暴露的服务类型、是否替换既有登记、类级拦截器
    /// </summary>
    /// <param name="ImplementationType">实现类型</param>
    /// <param name="Lifetime">生命周期</param>
    /// <param name="ServiceTypes">暴露的服务类型</param>
    /// <param name="ReplaceServices">是否替换既有登记</param>
    /// <param name="Interceptors">类级拦截器, 外层在前</param>
    /// <param name="HasInterceptors">类级或方法级是否声明了拦截器; 为 true 时接口服务类型生成代理</param>
    private readonly record struct ConventionalRegistration(
        Type ImplementationType,
        ServiceLifetime Lifetime,
        Type[] ServiceTypes,
        bool ReplaceServices,
        InterceptorAttribute[] Interceptors,
        bool HasInterceptors);
}
