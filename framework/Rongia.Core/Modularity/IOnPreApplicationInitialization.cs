using System.Threading.Tasks;

namespace Rongia.Modularity;

/// <summary>
/// 应用初始化前置阶段,容器已构建完成,所有模块的该方法都早于任何模块的 OnApplicationInitialization
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
