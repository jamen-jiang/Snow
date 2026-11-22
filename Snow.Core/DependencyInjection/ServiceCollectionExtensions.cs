using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Snow.Aop;

namespace Snow.DependencyInjection;

/// <summary>
/// 约定注册入口:每个程序集只枚举一次类型,收集完候选后按生命周期分桶登记 ——
/// 生命周期来自 <see cref="ServiceAttribute"/>(生命周期必须写在标注上,
/// <see cref="SingletonServiceAttribute"/> 等写明),暴露的服务类型来自
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
    /// <para>约定注册分两步: <see cref="CollectCandidates"/> 只扫描不写入, <see cref="ApplyConventionalRegistrations"/>
    /// 统一写入。模块加载器据此把全部扫描提前到任何模块的 ConfigureServices 之前, 显式登记于是稳定排在约定登记之后,
    /// 谁压过谁不再取决于模块顺序。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">待扫描程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddConventionalRegistrars(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        ApplyConventionalRegistrations(services, CollectCandidates(assemblies));

        return services;
    }

    /// <summary>
    /// 把一批扫描结果写入服务集合
    /// <para>写入发生在全部模块的 ConfigureServices 之后, 并遵守显式登记优先: 暴露自身的类型,
    /// 其服务类型在写入前已有登记就跳过, 宿主用 <c>TryAdd</c> / <c>Add</c> / <c>Replace</c> 都能顶掉默认实现,
    /// 也不会留下第二条死描述符; 只暴露外部契约的类型(如 IAuthorizationHandler 的多个实现)按实现类型去重后追加,
    /// 不会被已有登记挡掉。声明了 <see cref="ServiceAttribute.ReplaceServices"/> 的候选例外: 它的意图就是顶掉既有登记。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="candidates">扫描得到的候选登记</param>
    internal static void ApplyConventionalRegistrations(IServiceCollection services, IReadOnlyList<ConventionalRegistration> candidates)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            return;
        }

        // 写入前的登记快照: 显式登记(宿主或模块在 ConfigureServices 里写的)与框架自带登记都在里面
        HashSet<Type> presentServiceTypes = new(services.Select(descriptor => descriptor.ServiceType));

        // 按生命周期分桶追加,保持"单例 → 作用域 → 瞬态"的登记顺序
        foreach (ServiceLifetime lifetime in AllLifetimes)
        {
            foreach (ConventionalRegistration candidate in candidates)
            {
                if (candidate.Lifetime == lifetime && !candidate.ReplaceServices)
                {
                    AddDescriptors(services, candidate, presentServiceTypes);
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

            AddDescriptors(services, candidate, presentServiceTypes);
        }
    }

    /// <summary>
    /// 一趟扫描收集全部候选类型及其生命周期与暴露的服务类型
    /// </summary>
    /// <param name="assemblies">待扫描程序集</param>
    /// <returns>候选登记列表,顺序与程序集、类型声明顺序一致</returns>
    internal static List<ConventionalRegistration> CollectCandidates(params Assembly[] assemblies)
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
                    // 只打拦截器注解、没标服务注解时按瞬态登记, 做到"一个注解即可用"
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
    /// <para>同一实现类型同时暴露接口与自身时(如 ITokenService + TokenService), 接口描述符指向自身描述符的实例,
    /// 保证解析接口与解析具体类型拿到同一个对象, 不再各建一个。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="candidate">候选登记</param>
    /// <param name="presentServiceTypes">写入前的服务类型快照, 用于显式登记优先判定</param>
    private static void AddDescriptors(IServiceCollection services, ConventionalRegistration candidate, HashSet<Type> presentServiceTypes)
    {
        // 开放泛型定义交给代理工厂必然失败(代理需要具体类型), 在这里提前报错而不是等到第一次解析
        if (candidate.HasInterceptors && candidate.ImplementationType.IsGenericTypeDefinition)
        {
            throw new InvalidOperationException(
                "类型 " + candidate.ImplementationType.FullName + " 是开放泛型, 不能标拦截器注解: AOP 代理需要具体类型。");
        }

        // 既暴露接口又暴露自身的类型按"单一契约"处理: 任一服务类型已有登记就整条跳过, 显式登记优先
        bool exposesSelf = candidate.ServiceTypes.Contains(candidate.ImplementationType);

        // 单例/作用域下接口与自身共用一个实例; 瞬态本就每次新建, 不做重定向
        bool shareInstance = !candidate.HasInterceptors
            && !candidate.ImplementationType.IsGenericTypeDefinition
            && candidate.Lifetime != ServiceLifetime.Transient
            && exposesSelf
            && candidate.ServiceTypes.Length > 1;

        foreach (Type serviceType in candidate.ServiceTypes)
        {
            if (!candidate.ReplaceServices && presentServiceTypes.Contains(serviceType))
            {
                if (exposesSelf)
                {
                    continue;
                }

                // 只暴露外部契约的类型是"多实现"登记(如多个 IAuthorizationHandler), 只在实现类型重复时跳过
                if (HasDescriptor(services, serviceType, candidate.ImplementationType))
                {
                    continue;
                }
            }

            if (candidate.HasInterceptors)
            {
                // 代理只覆盖接口服务类型; 把实现类自身当作服务类型登记会拿到未被代理的实例, 因此跳过
                if (!serviceType.IsInterface)
                {
                    continue;
                }

                Type proxiedImplementationType = candidate.ImplementationType;
                InterceptorAttribute[] interceptors = candidate.Interceptors;

                services.Add(new ServiceDescriptor(
                    serviceType,
                    serviceProvider => CreateProxy(serviceProvider, proxiedImplementationType, serviceType, interceptors),
                    candidate.Lifetime));
                continue;
            }

            if (shareInstance && serviceType != candidate.ImplementationType)
            {
                Type targetType = candidate.ImplementationType;
                services.Add(new ServiceDescriptor(
                    serviceType,
                    serviceProvider => serviceProvider.GetRequiredService(targetType),
                    candidate.Lifetime));
                continue;
            }

            services.Add(new ServiceDescriptor(serviceType, candidate.ImplementationType, candidate.Lifetime));
        }
    }

    /// <summary>
    /// 判断集合里是否已有某服务类型指向该实现类型的描述符
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="serviceType">服务类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <returns>存在时返回 <see langword="true"/></returns>
    private static bool HasDescriptor(IServiceCollection services, Type serviceType, Type implementationType)
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == serviceType && descriptor.ImplementationType == implementationType)
            {
                return true;
            }
        }

        return false;
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
    /// 生命周期判定:读类上的服务注解;没标注解的类型不参与约定注册
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>生命周期;未标注解时返回 null</returns>
    private static ServiceLifetime? GetLifetime(Type type)
    {
        return GetServiceAttribute(type)?.Lifetime;
    }

    /// <summary>
    /// 读取类上的服务注解:从最派生的类型往上找, 命中第一层就停, 因此子类标的注解盖过基类标的
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>注解;未标时返回 null</returns>
    /// <exception cref="InvalidOperationException">同一个类型上标了多个服务注解</exception>
    private static ServiceAttribute? GetServiceAttribute(Type type)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            ServiceAttribute[] attributes = current.GetCustomAttributes<ServiceAttribute>(inherit: false).ToArray();

            if (attributes.Length == 0)
            {
                continue;
            }

            if (attributes.Length > 1)
            {
                throw new InvalidOperationException(
                    "类型 " + type.FullName + " 标了多个服务注解(" +
                    string.Join("、", attributes.Select(attribute => attribute.GetType().Name)) +
                    "): 一个类型只能标一个, 要替换既有登记就写在那一个上, 例如 [SingletonService(ReplaceServices = true)]。");
            }

            return attributes[0];
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
        return GetServiceAttribute(type)?.ReplaceServices == true;
    }

    /// <summary>
    /// 暴露的服务类型:由 <see cref="ExposedServiceExplorer"/> 决定
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>服务类型集合</returns>
    private static IEnumerable<Type> GetExposedServiceTypes(Type type)
    {
        return ExposedServiceExplorer.GetExposedServices(type);
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
    internal readonly record struct ConventionalRegistration(
        Type ImplementationType,
        ServiceLifetime Lifetime,
        Type[] ServiceTypes,
        bool ReplaceServices,
        InterceptorAttribute[] Interceptors,
        bool HasInterceptors);
}
