using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Snow;
using Snow.DependencyInjection;
using Snow.DynamicProxy;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 约定注册入口:每个程序集只枚举一次类型,收集完候选后按生命周期分桶登记 ——
/// 生命周期来自 <see cref="ServiceAttribute"/>(生命周期必须写在标注上,
/// <see cref="SingletonServiceAttribute"/> 等写明),暴露的服务类型来自
/// <see cref="ExposeServicesAttribute"/> 或默认推断(<see cref="ExposedServiceExplorer"/>)。
/// <para>拦截器来自各模块的登记器: 模块在 ConfigureServices 里用 <c>services.OnRegistered(...)</c> 挂上
/// <see cref="IOnServiceRegistredContext"/> 动作, 扫描到实现类型时由这些动作决定要不要挂拦截器 ——
/// 与 ABP 的 <c>AbpRegistrationBuilderExtensions.InvokeRegistrationActions</c> 同一机制。</para>
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

        bool classInterceptorsDisabled = services.GetRegistrationActionList().IsClassInterceptorsDisabled;

        // 按生命周期分桶追加,保持"单例 → 作用域 → 瞬态"的登记顺序
        foreach (ServiceLifetime lifetime in AllLifetimes)
        {
            foreach (ConventionalRegistration candidate in candidates)
            {
                if (candidate.Lifetime == lifetime && !candidate.ReplaceServices)
                {
                    AddDescriptors(services, candidate, presentServiceTypes, classInterceptorsDisabled);
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

            AddDescriptors(services, candidate, presentServiceTypes, classInterceptorsDisabled);
        }
    }

    /// <summary>
    /// 一趟扫描收集全部候选类型及其生命周期与暴露的服务类型
    /// </summary>
    /// <param name="services">服务集合, 用来取各模块挂上的登记动作</param>
    /// <param name="assemblies">待扫描程序集</param>
    /// <returns>候选登记列表,顺序与程序集、类型声明顺序一致</returns>
    internal static List<ConventionalRegistration> CollectCandidates(IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        ServiceRegistrationActionList registrationActionList = services.GetRegistrationActionList();
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
                if (!type.IsClass || type.IsAbstract)
                {
                    continue;
                }

                if (type.IsDefined(typeof(DisableConventionalRegistrationAttribute), true))
                {
                    continue;
                }

                ServiceLifetime? lifetime = GetLifetime(type);
                Type[] interceptorTypes = GetInterceptorTypes(registrationActionList, type);

                if (lifetime is null)
                {
                    // 只挂了拦截器、没标服务注解时按瞬态登记, 做到"一个注解即可用"
                    if (interceptorTypes.Length == 0)
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

                candidates.Add(new ConventionalRegistration(type, lifetime.Value, serviceTypes, IsReplacing(type), interceptorTypes));
            }
        }

        return candidates;
    }

    /// <summary>
    /// 把外部约定登记器送进来的类型并进候选列表
    /// <para>登记器只是把类型记成候选(见 <see cref="IConventionalRegistrar"/>), 与程序集扫描出来的候选在同一处补充拦截器:
    /// 用同一份登记动作清单跑 <see cref="GetInterceptorTypes"/> 判定, 因此外部登记的类型与标注解的类型得到完全一致的处理。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="candidates">已有候选列表, 就地追加</param>
    internal static void AppendRegistrarCandidates(IServiceCollection services, List<ConventionalRegistration> candidates)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(candidates);

        ServiceRegistrationActionList registrationActionList = services.GetRegistrationActionList();
        HashSet<Type> existingTypes = new(candidates.Select(candidate => candidate.ImplementationType));

        foreach (ExternalConventionalRegistration registration in services.GetExternalConventionalRegistrations())
        {
            if (!existingTypes.Add(registration.ImplementationType))
            {
                continue;
            }

            Type[] interceptorTypes = GetInterceptorTypes(registrationActionList, registration.ImplementationType);

            candidates.Add(new ConventionalRegistration(
                registration.ImplementationType,
                registration.Lifetime,
                registration.ServiceTypes,
                registration.ReplaceServices,
                interceptorTypes));
        }
    }

    /// <summary>
    /// 按服务类型追加描述符
    /// <para>同一个实现类型暴露多个服务类型时(如 IA + IB, 或接口 + 自身), 只产出一个实例: 先定下"锚点"服务类型并把实现登记到它,
    /// 其余服务类型都转发到锚点, 解析哪一条都拿到同一个对象 —— 单例/作用域不会被拆成两个实例, 瞬态也不会在一条解析链里造两份。</para>
    /// <para>挂了拦截器时锚点就是被代理的对象: 能类代理就锚在实现类型自身, 只能接口代理就锚在第一个接口, 其余服务类型一律转发到锚点。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="candidate">候选登记</param>
    /// <param name="presentServiceTypes">写入前的服务类型快照, 用于显式登记优先判定</param>
    /// <param name="classInterceptorsDisabled">类代理是否已全局关闭</param>
    private static void AddDescriptors(
        IServiceCollection services,
        ConventionalRegistration candidate,
        HashSet<Type> presentServiceTypes,
        bool classInterceptorsDisabled)
    {
        bool hasInterceptors = candidate.Interceptors.Length > 0;
        Type implementationType = candidate.ImplementationType;

        // 开放泛型定义交给代理工厂必然失败(代理需要具体类型), 在这里提前报错而不是等到第一次解析
        if (hasInterceptors && implementationType.IsGenericTypeDefinition)
        {
            throw new InvalidOperationException(
                "类型 " + implementationType.FullName + " 是开放泛型, 不能挂拦截器: AOP 代理需要具体类型。");
        }

        bool exposesSelf = candidate.ServiceTypes.Contains(implementationType);

        // 约定写入之前, 宿主(或别的模块)已经显式登记过实现类型自身; ReplaceServices 的候选会先清掉既有登记, 因此不算
        bool implementationExplicitlyRegistered = !candidate.ReplaceServices && presentServiceTypes.Contains(implementationType);

        // 类代理只在"实现类型是具体类且至少有一个可重写的虚方法"时建得出来; 全局关掉类代理时一律走接口代理
        bool classProxyable = hasInterceptors && !classInterceptorsDisabled && CanProxyClass(implementationType);

        // 本次真正要写的服务类型: 显式登记优先, 已有登记的服务类型不再覆盖; 实现类型自身由下面的锚点逻辑单独处理
        List<Type> targetServiceTypes = new();

        foreach (Type serviceType in candidate.ServiceTypes)
        {
            if (!candidate.ReplaceServices && presentServiceTypes.Contains(serviceType))
            {
                continue;
            }

            targetServiceTypes.Add(serviceType);
        }

        Type anchor;

        if (hasInterceptors && classProxyable)
        {
            // 类代理: 代理对象本身就是实现类型, 锚点落在实现类型上
            anchor = implementationType;

            if (implementationExplicitlyRegistered)
            {
                // 宿主显式登记过实现类型: 这里接管。否则解析具体类型拿到宿主的裸实例、解析接口拿到另一个代理对象, 变成两套状态
                RemoveDescriptors(services, implementationType);
            }

            services.Add(new ServiceDescriptor(
                implementationType,
                serviceProvider => CreateProxy(
                    serviceProvider, implementationType, implementationType, candidate.Interceptors),
                candidate.Lifetime));
        }
        else if (hasInterceptors)
        {
            // 类代理建不出来, 只能接口代理: 锚在第一个接口上, 其余接口都转发到它, 多接口也就一个被代理对象
            Type? firstInterface = targetServiceTypes.FirstOrDefault(serviceType => serviceType.IsInterface);

            if (firstInterface is null)
            {
                if (exposesSelf)
                {
                    // 标了拦截器、又暴露了自身、类代理却建不出来: 静默跳过的话这条服务彻底没人登记,
                    // 编译和启动都看不出问题, 第一次解析才报"没有以类型 X 注册的服务", 所以注册阶段直接报错
                    throw new InvalidOperationException(
                        "类型 " + implementationType.FullName + " 挂了拦截器, 又暴露了自身(按具体类型注入), 但它做不了类代理: "
                            + (classInterceptorsDisabled
                                ? "类代理已被 DisableSnowClassInterceptors 全局关闭。"
                                : implementationType.IsSealed
                                    ? "它是密封类, Castle 生成的代理类型要继承它, 继承不了。"
                                    : "它没有任何可重写的虚方法(Castle 的类代理只拦 virtual)。")
                            + " 两条出路: 一是让类代理建得出来 —— "
                            + (implementationType.IsSealed ? "先去掉 sealed, " : string.Empty)
                            + "把要拦的方法改成公开的 virtual(显式接口实现的私有方法要先改成公开虚方法, 注解可以继续留在接口方法上); "
                            + "二是去掉对自身的暴露, 只按接口解析 —— 接口代理不继承实现类型, 冒充不了具体类型, 兜不住按具体类型注入的调用。");
                }

                return;
            }

            anchor = firstInterface;

            Type serviceType = anchor;
            services.Add(new ServiceDescriptor(
                anchor,
                serviceProvider => CreateProxy(serviceProvider, implementationType, serviceType, candidate.Interceptors),
                candidate.Lifetime));
        }
        else if (implementationExplicitlyRegistered || exposesSelf)
        {
            // 没有拦截器, 实现类型自身可当锚点: 已显式登记就复用它, 否则自己登记
            anchor = implementationType;

            if (!implementationExplicitlyRegistered)
            {
                services.Add(new ServiceDescriptor(anchor, implementationType, candidate.Lifetime));
            }
        }
        else
        {
            // 只暴露外部契约(如 IAuthorizationHandler 的多个实现): 锚在第一个服务类型上, 其余转发到它
            if (targetServiceTypes.Count == 0)
            {
                return;
            }

            anchor = targetServiceTypes[0];
            services.Add(new ServiceDescriptor(anchor, implementationType, candidate.Lifetime));
        }

        Type anchorServiceType = anchor;

        foreach (Type serviceType in targetServiceTypes)
        {
            if (serviceType == anchorServiceType)
            {
                continue;
            }

            services.Add(new ServiceDescriptor(
                serviceType,
                serviceProvider => serviceProvider.GetRequiredService(anchorServiceType),
                candidate.Lifetime));
        }
    }

    /// <summary>
    /// 通过当前容器里的代理工厂创建代理
    /// </summary>
    private static object CreateProxy(
        IServiceProvider serviceProvider,
        Type implementationType,
        Type serviceType,
        IReadOnlyList<Type> interceptorTypes)
    {
        return serviceProvider.GetRequiredService<ISnowProxyFactory>()
            .CreateProxy(serviceProvider, implementationType, serviceType, interceptorTypes);
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
    /// 跑一遍登记动作, 取出该实现类型要挂的拦截器类型
    /// <para>动作只看实现类型(与 ABP 的登记器一致), 所以在扫描期按「实现类型同时当服务类型」跑一次即可,
    /// 不必等服务类型确定。带 <see cref="DisableSnowFeaturesAttribute.DisableInterceptors"/> 的类型直接放空。</para>
    /// </summary>
    /// <param name="registrationActionList">各模块挂上的登记动作</param>
    /// <param name="implementationType">实现类型</param>
    /// <returns>拦截器类型; 不拦截时返回空数组</returns>
    private static Type[] GetInterceptorTypes(ServiceRegistrationActionList registrationActionList, Type implementationType)
    {
        if (registrationActionList.Count == 0)
        {
            return Array.Empty<Type>();
        }

        OnServiceRegistredContext context = new(implementationType, implementationType);

        foreach (Action<IOnServiceRegistredContext> registrationAction in registrationActionList)
        {
            registrationAction.Invoke(context);
        }

        if (context.Interceptors.Count == 0)
        {
            return Array.Empty<Type>();
        }

        DisableSnowFeaturesAttribute? disableFeatures = implementationType.GetCustomAttribute<DisableSnowFeaturesAttribute>(true);
        if (disableFeatures is not null && disableFeatures.DisableInterceptors)
        {
            return Array.Empty<Type>();
        }

        return context.Interceptors.ToArray();
    }

    /// <summary>
    /// 判断实现类型能否做类代理: 必须是具体类, 且至少有一个可重写的虚方法(Castle 的类代理只拦 virtual)
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>可做类代理时返回 <see langword="true"/></returns>
    private static bool CanProxyClass(Type type)
    {
        if (type.IsSealed || type.IsAbstract)
        {
            return false;
        }

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.DeclaringType == typeof(object))
            {
                continue;
            }

            if (method.IsVirtual && !method.IsFinal)
            {
                return true;
            }
        }

        return false;
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
    /// 候选登记:实现类型、生命周期、暴露的服务类型、是否替换既有登记、要挂的拦截器类型
    /// </summary>
    /// <param name="ImplementationType">实现类型</param>
    /// <param name="Lifetime">生命周期</param>
    /// <param name="ServiceTypes">暴露的服务类型</param>
    /// <param name="ReplaceServices">是否替换既有登记</param>
    /// <param name="Interceptors">拦截器类型, 外层在前</param>
    internal readonly record struct ConventionalRegistration(
        Type ImplementationType,
        ServiceLifetime Lifetime,
        Type[] ServiceTypes,
        bool ReplaceServices,
        Type[] Interceptors);
}
