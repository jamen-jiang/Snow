namespace Rongia.Aop;

/// <summary>
/// 代理工厂: 为一个实现类型生成代理对象, 代理上的每次调用都经过给定的拦截器.
/// </summary>
public interface IAopProxyFactory
{
    /// <summary>
    /// 创建代理
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="implementationType">实现类型</param>
    /// <param name="serviceType">暴露的接口服务类型</param>
    /// <param name="interceptors">类级拦截器, 按外层到内层排列</param>
    /// <returns>代理对象</returns>
    object CreateProxy(IServiceProvider serviceProvider, Type implementationType, Type serviceType, InterceptorAttribute[] interceptors);
}
