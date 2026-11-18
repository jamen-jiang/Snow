using System.Reflection;

namespace Rongia.DependencyInjection;

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
    /// 默认推断规则:只暴露与实现类型处于同一程序集、且名称以大写 I 开头的接口(泛型按定义名判断)。
    /// 同程序集判据把 BCL 与第三方库的接口(IDisposable、IComparable、ILogger 等)排除在外,
    /// 避免实现类因为顺带实现了某个框架接口而被登记成该框架接口的服务
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>接口列表</returns>
    public static List<Type> GetDefaultServices(Type type)
    {
        List<Type> serviceTypes = new List<Type>();
        Assembly implementationAssembly = type.Assembly;

        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (interfaceType.Assembly != implementationAssembly)
            {
                continue;
            }

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

            if (!serviceTypes.Contains(interfaceType))
            {
                serviceTypes.Add(interfaceType);
            }
        }

        return serviceTypes;
    }
}
