namespace Snow.Modularity;

/// <summary>
/// 应用关停阶段(对应 ABP 的 IOnApplicationShutdown): 容器还没释放, 服务仍可用
/// </summary>
public interface IOnApplicationShutdown
{
    /// <summary>
    /// 应用关停阶段
    /// </summary>
    /// <param name="context">应用关停上下文</param>
    void OnApplicationShutdown(ApplicationShutdownContext context);

    /// <summary>
    /// 应用关停阶段的异步版本
    /// </summary>
    /// <param name="context">应用关停上下文</param>
    /// <returns>表示关停过程的任务</returns>
    Task OnApplicationShutdownAsync(ApplicationShutdownContext context);
}
