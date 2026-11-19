namespace Rongia.Modularity;

/// <summary>
/// 应用初始化的后置阶段(对应 ABP 的 IOnPostApplicationInitialization):
/// 在全部模块的 OnApplicationInitialization 之后执行
/// </summary>
public interface IOnPostApplicationInitialization
{
    /// <summary>
    /// 后置初始化阶段的异步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context);

    /// <summary>
    /// 后置初始化阶段
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    void OnPostApplicationInitialization(ApplicationInitializationContext context);
}
