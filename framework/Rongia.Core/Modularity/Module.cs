namespace Rongia.Modularity;

/// <summary>
/// 模块基类。一个模块代表一个可复用的功能单元(通常对应一个程序集),
/// 通过重写各阶段的钩子来注册服务与配置中间件。
/// 加载顺序由 <see cref="DependsOnAttribute"/> 描述,加载器负责拓扑排序。
/// </summary>
public abstract class Module
{
    /// <summary>
    /// 服务注册前置阶段:所有模块的该方法都会早于任何模块的 ConfigureServices 执行,
    /// 适合做需要抢占位置的注册(例如替换默认实现)。
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void PreConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 服务注册阶段:约定注册扫描本模块程序集之后调用,
    /// 因此这里显式登记的服务会优先于约定注册生效。
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void ConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 服务注册后置阶段:所有模块的 ConfigureServices 都执行完之后调用。
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void PostConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 应用初始化前置阶段,容器已构建完成。
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnPreApplicationInitialization(ApplicationInitializationContext context) { }

    /// <summary>
    /// 应用初始化阶段:适合挂中间件、订阅事件、预热缓存等需要容器的动作。
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnApplicationInitialization(ApplicationInitializationContext context) { }

    /// <summary>
    /// 应用初始化后置阶段:所有模块的 OnApplicationInitialization 都执行完之后调用。
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnPostApplicationInitialization(ApplicationInitializationContext context) { }

    /// <summary>
    /// 应用关闭阶段:按加载顺序的逆序执行,适合释放外部资源。
    /// </summary>
    /// <param name="context">应用关闭上下文</param>
    public virtual void OnApplicationShutdown(ApplicationShutdownContext context) { }
}