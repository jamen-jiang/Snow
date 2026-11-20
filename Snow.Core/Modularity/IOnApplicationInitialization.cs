using System.Threading.Tasks;

namespace Snow.Modularity;

/// <summary>
/// 应用初始化阶段:适合挂中间件、订阅事件、预热缓存等需要容器的动作
/// </summary>
public interface IOnApplicationInitialization
{
    /// <summary>
    /// 初始化阶段的异步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    Task OnApplicationInitializationAsync(ApplicationInitializationContext context);

    /// <summary>
    /// 初始化阶段
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    void OnApplicationInitialization(ApplicationInitializationContext context);
}
