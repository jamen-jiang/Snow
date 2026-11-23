namespace Snow.DependencyInjection;

/// <summary>
/// 服务提供者访问器契约(对应 ABP 的 <c>IServiceProviderAccessor</c>):
/// 任何在某个阶段持有服务提供者的上下文对象都实现它, 于是取当前请求、当前用户这类扩展只写一遍
/// </summary>
public interface IServiceProviderAccessor
{
    /// <summary>
    /// 当前阶段的服务提供者
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}
