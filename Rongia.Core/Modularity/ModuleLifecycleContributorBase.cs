namespace Rongia.Modularity;

/// <summary>
/// 生命周期贡献者的空实现(对应 ABP 的 ModuleLifecycleContributorBase):
/// 具体贡献者只重写自己关心的那一对方法
/// </summary>
public abstract class ModuleLifecycleContributorBase : IModuleLifecycleContributor
{
    /// <inheritdoc />
    public virtual Task InitializeAsync(ApplicationInitializationContext context, RongiaModule module)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void Initialize(ApplicationInitializationContext context, RongiaModule module) { }

    /// <inheritdoc />
    public virtual Task ShutdownAsync(ApplicationShutdownContext context, RongiaModule module)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void Shutdown(ApplicationShutdownContext context, RongiaModule module) { }
}
