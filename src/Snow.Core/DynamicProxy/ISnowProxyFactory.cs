namespace Snow.DynamicProxy;

/// <summary>
/// 代理工厂: Core 只负责决定哪个服务需要代理, 具体代理引擎由 Snow.Castle 提供。
/// </summary>
public interface ISnowProxyFactory
{
    /// <summary>
    /// 创建代理对象
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="implementationType">实现类型</param>
    /// <param name="serviceType">登记的服务类型; 接口走接口代理, 具体类走类代理</param>
    /// <param name="interceptorTypes">拦截器类型</param>
    /// <returns>代理对象</returns>
    object CreateProxy(
        IServiceProvider serviceProvider,
        Type implementationType,
        Type serviceType,
        IReadOnlyList<Type> interceptorTypes);
}
