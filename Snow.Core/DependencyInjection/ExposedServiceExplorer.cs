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
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>接口列表</returns>
    public static List<Type> GetDefaultServices(Type type)
    {
        List<Type> serviceTypes = new List<Type>();

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
            if (!type.Name.EndsWith(name.Substring(1), StringComparison.Ordinal))
            {
                continue;
            }

            if (!serviceTypes.Contains(interfaceType))
            {
                serviceTypes.Add(interfaceType);
            }
        }

        return serviceTypes;
    }
}
