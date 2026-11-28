using Snow.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 登记动作扩展(对应 ABP 的 <c>ServiceCollectionRegistrationActionExtensions.OnRegistered</c>)
/// </summary>
public static class ServiceCollectionRegistrationActionExtensions
{
    /// <summary>
    /// 挂一个服务登记动作: 约定注册扫描到某个实现类型时调用, 由它决定加哪些拦截器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="registrationAction">登记动作</param>
    public static void OnRegistered(this IServiceCollection services, Action<IOnServiceRegistredContext> registrationAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registrationAction);

        GetOrCreateRegistrationActionList(services).Add(registrationAction);
    }

    /// <summary>
    /// 读取登记动作清单(没有就建一个空清单)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>登记动作清单</returns>
    public static ServiceRegistrationActionList GetRegistrationActionList(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return GetOrCreateRegistrationActionList(services);
    }

    /// <summary>
    /// 关闭类代理: 标了拦截器的类型只做接口代理
    /// </summary>
    /// <param name="services">服务集合</param>
    public static void DisableSnowClassInterceptors(this IServiceCollection services)
    {
        GetOrCreateRegistrationActionList(services).IsClassInterceptorsDisabled = true;
    }

    /// <summary>
    /// 类代理是否已关闭
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>已关闭时返回 true</returns>
    public static bool IsSnowClassInterceptorsDisabled(this IServiceCollection services)
    {
        return GetOrCreateRegistrationActionList(services).IsClassInterceptorsDisabled;
    }

    /// <summary>
    /// 取服务集合里登记动作清单的单例; 没有就新建并登记成单例
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>登记动作清单</returns>
    private static ServiceRegistrationActionList GetOrCreateRegistrationActionList(IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(ServiceRegistrationActionList) &&
                descriptor.ImplementationInstance is ServiceRegistrationActionList existing)
            {
                return existing;
            }
        }

        ServiceRegistrationActionList actionList = new();
        services.AddSingleton(actionList);

        return actionList;
    }
}
