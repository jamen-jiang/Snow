using System.Threading.Tasks;

namespace Rongia.Modularity;

/// <summary>
/// 应用初始化后置阶段,所有模块的 OnApplicationInitialization 都执行完之后调用
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
