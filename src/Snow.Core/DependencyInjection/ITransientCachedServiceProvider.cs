namespace Snow.DependencyInjection;

/// <summary>
/// 缓存解析结果的服务提供者契约(对应 ABP 的 <c>ITransientCachedServiceProvider</c>):
/// 生命周期是瞬态, 每个注入点各有一份缓存
/// </summary>
public interface ITransientCachedServiceProvider : ICachedServiceProviderBase
{
}
