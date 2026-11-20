using System.Threading.Tasks;

namespace Snow.Modularity;

/// <summary>
/// 应用关闭阶段:按加载顺序的逆序执行,适合释放外部资源
/// </summary>
public interface IOnApplicationShutdown
{
    /// <summary>
    /// 关闭阶段的异步版本
    /// </summary>
    /// <param name="context">应用关闭上下文</param>
    /// <returns>表示关闭过程的任务</returns>
    Task OnApplicationShutdownAsync(ApplicationShutdownContext context);

    /// <summary>
    /// 关闭阶段
    /// </summary>
    /// <param name="context">应用关闭上下文</param>
    void OnApplicationShutdown(ApplicationShutdownContext context);
}
