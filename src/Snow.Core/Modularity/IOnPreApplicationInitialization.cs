namespace Snow.Modularity;

/// <summary>
/// 应用预初始化阶段(对应 ABP 的 IOnPreApplicationInitialization): 容器已构建、管道骨架未展开
/// </summary>
public interface IOnPreApplicationInitialization
{
    /// <summary>
    /// 应用预初始化阶段
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    void OnPreApplicationInitialization(ApplicationInitializationContext context);

    /// <summary>
    /// 应用预初始化阶段的异步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context);
}
