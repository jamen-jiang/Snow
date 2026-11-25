namespace Snow.DependencyInjection;

/// <summary>
/// 缓存解析结果的服务提供者(对应 ABP 的 <c>TransientCachedServiceProvider</c>): 瞬态生命周期
/// </summary>
[TransientService]
[ExposeServices(typeof(ITransientCachedServiceProvider))]
public class TransientCachedServiceProvider : CachedServiceProviderBase, ITransientCachedServiceProvider
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">当前作用域的服务提供者</param>
    public TransientCachedServiceProvider(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }
}
