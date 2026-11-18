namespace Rongia.DependencyInjection;

/// <summary>
/// 指定实现类型对外暴露的服务类型,覆盖默认的"以 I 开头的接口加自身"(对应 ABP ExposeServicesAttribute)
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ExposeServicesAttribute : Attribute, IExposedServiceTypesProvider
{
    /// <summary>
    /// 显式指定的服务类型
    /// </summary>
    public Type[] ServiceTypes { get; }

    /// <summary>
    /// 是否附加默认推断出的接口(默认 false:只暴露显式列出的类型)
    /// </summary>
    public bool IncludeDefaults { get; set; }

    /// <summary>
    /// 是否附加实现类型自身,便于直接按具体类型注入
    /// </summary>
    public bool IncludeSelf { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceTypes">服务类型列表</param>
    public ExposeServicesAttribute(params Type[] serviceTypes)
    {
        ServiceTypes = serviceTypes ?? Type.EmptyTypes;
    }

    /// <summary>
    /// 获取目标类型应对外暴露的服务类型
    /// </summary>
    /// <param name="targetType">实现类型</param>
    /// <returns>服务类型集合</returns>
    public Type[] GetExposedServiceTypes(Type targetType)
    {
        List<Type> services = new List<Type>(ServiceTypes);

        if (IncludeDefaults)
        {
            foreach (Type serviceType in ExposedServiceExplorer.GetDefaultServices(targetType))
            {
                if (!services.Contains(serviceType))
                {
                    services.Add(serviceType);
                }
            }
        }

        if (IncludeSelf && !services.Contains(targetType))
        {
            services.Add(targetType);
        }

        return services.ToArray();
    }
}
