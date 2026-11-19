namespace Rongia.Modularity;

/// <summary>
/// 前置初始化阶段贡献者(对应 ABP 的 OnPreApplicationInitializationModuleLifecycleContributor)
/// </summary>
public sealed class OnPreApplicationInitializationModuleLifecycleContributor : ModuleLifecycleContributorBase
{
    /// <inheritdoc />
    public override async Task InitializeAsync(ApplicationInitializationContext context, RongiaModule module)
    {
        if (module is IOnPreApplicationInitialization initialization)
        {
            await initialization.OnPreApplicationInitializationAsync(context);
        }
    }

    /// <inheritdoc />
    public override void Initialize(ApplicationInitializationContext context, RongiaModule module)
    {
        (module as IOnPreApplicationInitialization)?.OnPreApplicationInitialization(context);
    }
}

/// <summary>
/// 初始化阶段贡献者(对应 ABP 的 OnApplicationInitializationModuleLifecycleContributor)
/// </summary>
public sealed class OnApplicationInitializationModuleLifecycleContributor : ModuleLifecycleContributorBase
{
    /// <inheritdoc />
    public override async Task InitializeAsync(ApplicationInitializationContext context, RongiaModule module)
    {
        if (module is IOnApplicationInitialization initialization)
        {
            await initialization.OnApplicationInitializationAsync(context);
        }
    }

    /// <inheritdoc />
    public override void Initialize(ApplicationInitializationContext context, RongiaModule module)
    {
        (module as IOnApplicationInitialization)?.OnApplicationInitialization(context);
    }
}

/// <summary>
/// 后置初始化阶段贡献者(对应 ABP 的 OnPostApplicationInitializationModuleLifecycleContributor)
/// </summary>
public sealed class OnPostApplicationInitializationModuleLifecycleContributor : ModuleLifecycleContributorBase
{
    /// <inheritdoc />
    public override async Task InitializeAsync(ApplicationInitializationContext context, RongiaModule module)
    {
        if (module is IOnPostApplicationInitialization initialization)
        {
            await initialization.OnPostApplicationInitializationAsync(context);
        }
    }

    /// <inheritdoc />
    public override void Initialize(ApplicationInitializationContext context, RongiaModule module)
    {
        (module as IOnPostApplicationInitialization)?.OnPostApplicationInitialization(context);
    }
}

/// <summary>
/// 关闭阶段贡献者(对应 ABP 的 OnApplicationShutdownModuleLifecycleContributor)
/// </summary>
public sealed class OnApplicationShutdownModuleLifecycleContributor : ModuleLifecycleContributorBase
{
    /// <inheritdoc />
    public override async Task ShutdownAsync(ApplicationShutdownContext context, RongiaModule module)
    {
        if (module is IOnApplicationShutdown shutdown)
        {
            await shutdown.OnApplicationShutdownAsync(context);
        }
    }

    /// <inheritdoc />
    public override void Shutdown(ApplicationShutdownContext context, RongiaModule module)
    {
        (module as IOnApplicationShutdown)?.OnApplicationShutdown(context);
    }
}
