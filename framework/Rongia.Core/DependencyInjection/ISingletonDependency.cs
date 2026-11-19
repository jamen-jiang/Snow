namespace Rongia.DependencyInjection;

/// <summary>
/// 单例生命周期标记接口:实现它的类型在约定注册时按 Singleton 注册(对应 ABP ISingletonDependency)
/// </summary>
/// <remarks>单例中不可直接注入 Scoped 服务,否则形成 captive dependency;需要时改为注入 IServiceProvider 或按需开作用域解析。</remarks>
public interface ISingletonDependency
{
}
