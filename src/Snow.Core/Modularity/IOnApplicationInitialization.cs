namespace Snow.Modularity;

/// <summary>
/// 应用初始化第一段(对应 ABP 的 IOnApplicationInitialization): 管道骨架仍未展开
/// </summary>
public interface IOnApplicationInitialization
{
    /// <summary>
    /// 应用初始化第一段
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    void OnApplicationInitialization(ApplicationInitializationContext context);

    /// <summary>
    /// 应用初始化第一段的异步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    Task OnApplicationInitializationAsync(ApplicationInitializationContext context);
}
