using Snow.Collections;
using Snow.DynamicProxy;

namespace Snow.DependencyInjection;

/// <summary>
/// 一个服务刚要登记时的上下文(对应 ABP 的 <c>IOnServiceRegistredContext</c>):
/// 登记器(registrar)在这里读实现类型, 决定要不要给它加拦截器。
/// </summary>
public interface IOnServiceRegistredContext
{
    /// <summary>
    /// 本次登记要挂上的拦截器类型清单, 由各模块的登记器往里加
    /// </summary>
    ITypeList<ISnowInterceptor> Interceptors { get; }

    /// <summary>
    /// 实现类型
    /// </summary>
    Type ImplementationType { get; }

    /// <summary>
    /// 服务键(键控服务时非空)
    /// </summary>
    object? ServiceKey { get; }
}
