namespace Snow.DependencyInjection;

/// <summary>
/// 暴露服务类型解析器:优先读取 ExposeServicesAttribute,否则按默认规则推断
/// </summary>
public static class ExposedServiceExplorer
{
    private static readonly ExposeServicesAttribute DefaultExposeServices = new ExposeServicesAttribute
    {
        IncludeDefaults = true,
        IncludeSelf = true,
    };

    /// <summary>
    /// 获取某个实现类型应对外暴露的服务类型
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>服务类型集合</returns>
    public static Type[] GetExposedServices(Type type)
    {
        IExposedServiceTypesProvider? provider = type
            .GetCustomAttributes(typeof(IExposedServiceTypesProvider), true)
            .Cast<IExposedServiceTypesProvider>()
            .FirstOrDefault();

        IExposedServiceTypesProvider effectiveProvider = provider ?? DefaultExposeServices;
        return effectiveProvider.GetExposedServiceTypes(type);
    }

    /// <summary>
    /// 默认推断规则:接口名去掉开头的 I 之后必须以实现类型名结尾(泛型按定义名判断),对齐 ABP 的命名后缀判据。
    /// 该判据跨程序集有效,同时把 BCL 与第三方库的接口(IDisposable、IComparable、ILogger 等)排除在外,
    /// 避免实现类因为顺带实现了某个框架接口而被登记成该框架接口的服务
    /// <para>开放泛型实现类:类名先去掉参数计数后缀再比对,泛型参数个数必须与接口一致,
    /// 且服务类型取泛型定义(如 IDataFilter&lt;&gt;)而不是构造类型,否则容器闭合时无法实例化。</para>
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>接口列表</returns>
    public static List<Type> GetDefaultServices(Type type)
    {
        List<Type> serviceTypes = new List<Type>();

        // 开放泛型实现类的类名带 `N 参数计数后缀(如 DataFilter`1), 判后缀前先去掉
        string implementationName = type.Name;
        int implementationTickIndex = implementationName.IndexOf((char)96);
        if (implementationTickIndex > 0)
        {
            implementationName = implementationName.Substring(0, implementationTickIndex);
        }

        foreach (Type interfaceType in type.GetInterfaces())
        {
            Type definition = interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
            string name = definition.Name;

            int tickIndex = name.IndexOf((char)96);
            if (tickIndex > 0)
            {
                name = name.Substring(0, tickIndex);
            }

            if (name.Length <= 1 || !name.StartsWith("I", StringComparison.Ordinal))
            {
                continue;
            }

            // 去掉开头的 I 之后必须是实现类名的后缀, 才认定它是该类对外暴露的默认服务类型
            if (!implementationName.EndsWith(name.Substring(1), StringComparison.Ordinal))
            {
                continue;
            }

            Type serviceType = interfaceType;

            if (type.IsGenericTypeDefinition)
            {
                // 参数必须能一一对应: 接口自身也得是开放泛型, 且泛型参数个数与实现类一致
                // (如 DistributedCache<TCacheItem> 还实现了 IDistributedCache<TCacheItem, string>, 那个不能算)
                if (!interfaceType.IsGenericType || !interfaceType.ContainsGenericParameters)
                {
                    continue;
                }

                if (definition.GetGenericArguments().Length != type.GetGenericArguments().Length)
                {
                    continue;
                }

                // 开放泛型只能按泛型定义登记: 构造类型(IDataFilter<TFilter>)容器闭合时无法实例化
                serviceType = definition;
            }

            if (!serviceTypes.Contains(serviceType))
            {
                serviceTypes.Add(serviceType);
            }
        }

        return serviceTypes;
    }
}
