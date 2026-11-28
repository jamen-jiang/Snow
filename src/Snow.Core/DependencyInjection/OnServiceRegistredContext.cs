using Snow.Collections;
using Snow.DynamicProxy;

namespace Snow.DependencyInjection;

/// <summary>
/// <see cref="IOnServiceRegistredContext"/> 的默认实现(对应 ABP 的 <c>OnServiceRegistredContext</c>)
/// </summary>
public class OnServiceRegistredContext : IOnServiceRegistredContext
{
    /// <inheritdoc />
    public virtual ITypeList<ISnowInterceptor> Interceptors { get; }

    /// <summary>
    /// 服务类型
    /// </summary>
    public virtual Type ServiceType { get; }

    /// <inheritdoc />
    public virtual Type ImplementationType { get; }

    /// <inheritdoc />
    public virtual object? ServiceKey { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <param name="serviceKey">服务键</param>
    public OnServiceRegistredContext(Type serviceType, Type implementationType, object? serviceKey = null)
    {
        ServiceType = Check.NotNull(serviceType, nameof(serviceType));
        ImplementationType = Check.NotNull(implementationType, nameof(implementationType));
        ServiceKey = serviceKey;

        Interceptors = new TypeList<ISnowInterceptor>();
    }
}
