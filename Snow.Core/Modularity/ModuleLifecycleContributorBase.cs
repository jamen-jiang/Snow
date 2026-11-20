namespace Snow.Modularity;

/// <summary>
/// 生命周期贡献者的空实现(对应 ABP 的 ModuleLifecycleContributorBase):
/// 具体贡献者只重写自己关心的那一对方法
/// </summary>
public abstract class ModuleLifecycleContributorBase : IModuleLifecycleContributor
{
    /// <inheritdoc />
    public virtual Task InitializeAsync(ApplicationInitializationContext context, SnowModule module)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void Initialize(ApplicationInitializationContext context, SnowModule module) { }

    /// <inheritdoc />
    public virtual Task ShutdownAsync(ApplicationShutdownContext context, SnowModule module)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void Shutdown(ApplicationShutdownContext context, SnowModule module) { }
}
