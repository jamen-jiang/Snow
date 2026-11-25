namespace Snow.DependencyInjection;

/// <summary>
/// 缓存解析结果的服务提供者(对应 ABP 的 <c>CachedServiceProvider</c>): 作用域生命周期
/// </summary>
[ScopedService]
[ExposeServices(typeof(ICachedServiceProvider))]
public class CachedServiceProvider : CachedServiceProviderBase, ICachedServiceProvider
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">当前作用域的服务提供者</param>
    public CachedServiceProvider(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }
}
