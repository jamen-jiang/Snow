namespace Snow.Modularity;

/// <summary>
/// 模块生命周期管理器(对应 ABP 的 IModuleManager):
/// 按贡献者顺序驱动全部模块的初始化与关闭
/// </summary>
public interface IModuleManager
{
    /// <summary>
    /// 驱动全部模块的初始化阶段
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    Task InitializeModulesAsync(ApplicationInitializationContext context);

    /// <summary>
    /// 驱动全部模块的初始化阶段(同步)
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    void InitializeModules(ApplicationInitializationContext context);

    /// <summary>
    /// 驱动全部模块的关闭阶段,按加载顺序的逆序执行
    /// </summary>
    /// <param name="context">应用关闭上下文</param>
    /// <returns>表示关闭过程的任务</returns>
    Task ShutdownModulesAsync(ApplicationShutdownContext context);

    /// <summary>
    /// 驱动全部模块的关闭阶段(同步),按加载顺序的逆序执行
    /// </summary>
    /// <param name="context">应用关闭上下文</param>
    void ShutdownModules(ApplicationShutdownContext context);
}
