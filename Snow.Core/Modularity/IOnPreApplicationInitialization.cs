namespace Snow.Modularity;

/// <summary>
/// 应用初始化的前置阶段(对应 ABP 的 IOnPreApplicationInitialization):
/// 在全部模块的 OnApplicationInitialization 之前执行
/// </summary>
public interface IOnPreApplicationInitialization
{
    /// <summary>
    /// 前置初始化阶段的异步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context);

    /// <summary>
    /// 前置初始化阶段
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    void OnPreApplicationInitialization(ApplicationInitializationContext context);
}
