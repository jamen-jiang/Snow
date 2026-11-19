using System.Threading.Tasks;

namespace Rongia.Modularity;

/// <summary>
/// 模块基类。一个模块代表一个可复用的功能单元(通常对应一个程序集),
/// 通过重写各阶段的钩子来注册服务与配置中间件。
/// 加载顺序由 <see cref="DependsOnAttribute"/> 描述,加载器负责拓扑排序。
/// <para>本类把 <see cref="IModule"/> 与各生命周期接口的空实现一次给全,
/// 派生类只需重写关心的钩子。</para>
/// </summary>
public abstract class Module :
    IModule,
    IOnPreApplicationInitialization,
    IOnApplicationInitialization,
    IOnPostApplicationInitialization,
    IOnApplicationShutdown
{
    /// <summary>
    /// 服务注册阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    public virtual Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        ConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 服务注册阶段:约定扫描本模块程序集之后调用,
    /// 因此这里显式登记的服务会优先于约定注册生效。
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void ConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 服务注册前置阶段:所有模块的该方法都会早于任何模块的 ConfigureServices 执行,
    /// 适合做需要抢占位置的注册(例如替换默认实现)。
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void PreConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 服务注册后置阶段:所有模块的 ConfigureServices 都执行完之后调用。
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void PostConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 应用初始化前置阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    public virtual Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnPreApplicationInitialization(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 应用初始化前置阶段,容器已构建完成。
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnPreApplicationInitialization(ApplicationInitializationContext context) { }

    /// <summary>
    /// 应用初始化阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    public virtual Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnApplicationInitialization(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 应用初始化阶段:适合挂中间件、订阅事件、预热缓存等需要容器的动作。
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnApplicationInitialization(ApplicationInitializationContext context) { }

    /// <summary>
    /// 应用初始化后置阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    public virtual Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnPostApplicationInitialization(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 应用初始化后置阶段:所有模块的 OnApplicationInitialization 都执行完之后调用。
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnPostApplicationInitialization(ApplicationInitializationContext context) { }

    /// <summary>
    /// 应用关闭阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">应用关闭上下文</param>
    /// <returns>表示关闭过程的任务</returns>
    public virtual Task OnApplicationShutdownAsync(ApplicationShutdownContext context)
    {
        OnApplicationShutdown(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 应用关闭阶段:按加载顺序的逆序执行,适合释放外部资源。
    /// </summary>
    /// <param name="context">应用关闭上下文</param>
    public virtual void OnApplicationShutdown(ApplicationShutdownContext context) { }
}
