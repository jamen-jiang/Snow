namespace Snow.DependencyInjection;

/// <summary>
/// 缓存解析结果的服务提供者契约(对应 ABP 的 <c>ICachedServiceProvider</c>):
/// 生命周期是作用域, 一个作用域内所有解析结果共享同一份缓存
/// </summary>
public interface ICachedServiceProvider : ICachedServiceProviderBase
{
}
