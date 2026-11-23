namespace Snow.Modularity;

/// <summary>
/// 应用初始化收尾段(对应 ABP 的 IOnPostApplicationInitialization): 骨架展开之后、端点映射之前
/// </summary>
public interface IOnPostApplicationInitialization
{
    /// <summary>
    /// 应用初始化收尾段
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    void OnPostApplicationInitialization(ApplicationInitializationContext context);

    /// <summary>
    /// 应用初始化收尾段的异步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context);
}
