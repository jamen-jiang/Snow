namespace Rongia.Modularity;

/// <summary>
/// 模块生命周期贡献者(对应 ABP 的 IModuleLifecycleContributor):
/// 一个贡献者负责一个阶段,由 <see cref="IModuleManager"/> 按注册顺序驱动,
/// 这样"阶段"本身是可插拔的,新增阶段不必改应用类
/// </summary>
public interface IModuleLifecycleContributor
{
    /// <summary>
    /// 初始化阶段的异步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <param name="module">当前模块</param>
    /// <returns>表示初始化过程的任务</returns>
    Task InitializeAsync(ApplicationInitializationContext context, RongiaModule module);

    /// <summary>
    /// 初始化阶段
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <param name="module">当前模块</param>
    void Initialize(ApplicationInitializationContext context, RongiaModule module);

    /// <summary>
    /// 关闭阶段的异步版本
    /// </summary>
    /// <param name="context">应用关闭上下文</param>
    /// <param name="module">当前模块</param>
    /// <returns>表示关闭过程的任务</returns>
    Task ShutdownAsync(ApplicationShutdownContext context, RongiaModule module);

    /// <summary>
    /// 关闭阶段
    /// </summary>
    /// <param name="context">应用关闭上下文</param>
    /// <param name="module">当前模块</param>
    void Shutdown(ApplicationShutdownContext context, RongiaModule module);
}
