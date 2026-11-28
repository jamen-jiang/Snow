using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块基类(对应 ABP 的 AbpModule)。一个模块代表一个可复用的功能单元(通常对应一个程序集)。
/// <para>生命周期两段: 服务注册阶段(<see cref="PreConfigureServices"/> / <see cref="ConfigureServices"/> / <see cref="PostConfigureServices"/>)
/// 与容器构建之后的应用初始化阶段(<see cref="OnPreApplicationInitialization"/> / <see cref="OnApplicationInitialization"/> /
/// <see cref="OnPostApplicationInitialization"/>); 应用关停时跑 <see cref="OnApplicationShutdown"/>。</para>
/// <para>每个动作都有同步与异步两个版本, 异步版本默认调用同步版本, 重写任意一个都生效(与 ABP 一致)。</para>
/// <para>模块不铺管道: 整条管道由宿主在启动模块的 <c>OnApplicationInitialization</c> 里按行序手写(对齐 ABP 模板)。
/// 能力包要加中间件只提供 <c>UseXxx()</c> 扩展方法, 挂不挂、挂在哪一行由宿主决定。</para>
/// <para>加载顺序由 <see cref="DependsOnAttribute"/> 描述, 加载器负责拓扑排序; 同一阶段内的先后 = 拓扑序。</para>
/// </summary>
public abstract class SnowModule :
    ISnowModule,
    IPreConfigureServices,
    IPostConfigureServices,
    IOnPreApplicationInitialization,
    IOnApplicationInitialization,
    IOnPostApplicationInitialization,
    IOnApplicationShutdown
{
    private ServiceConfigurationContext? _serviceConfigurationContext;

    /// <summary>
    /// 为 true 时本模块所在程序集不做约定扫描,服务全部由模块自己显式登记
    /// </summary>
    protected internal bool SkipAutoServiceRegistration { get; protected set; }

    /// <summary>
    /// 当前的服务配置上下文,只在服务注册阶段(前 / 中 / 后三段)可用
    /// </summary>
    /// <exception cref="SnowInitializationException">在服务注册阶段之外访问</exception>
    [System.Diagnostics.CodeAnalysis.AllowNull]
    protected internal ServiceConfigurationContext ServiceConfigurationContext
    {
        get
        {
            if (_serviceConfigurationContext is null)
            {
                throw new SnowInitializationException(
                    nameof(ServiceConfigurationContext) + " 只在 " + nameof(ConfigureServices) + " 阶段可用。");
            }

            return _serviceConfigurationContext;
        }

        internal set => _serviceConfigurationContext = value;
    }

    /// <summary>
    /// 服务注册前段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    public virtual Task PreConfigureServicesAsync(ServiceConfigurationContext context)
    {
        PreConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 服务注册前段: 在所有模块的 <see cref="ConfigureServices"/> 之前执行。
    /// <para>适合放"必须在别人注册之前就位"的东西, 例如给宿主或依赖模块预留的占位登记。
    /// 这个阶段不做约定扫描, 写在这里的登记一定排在约定登记之前。</para>
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void PreConfigureServices(ServiceConfigurationContext context) { }

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
    /// 服务注册中段:约定扫描本模块程序集之后调用,
    /// 因此这里显式登记的服务会优先于约定注册生效。
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void ConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 服务注册后段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    public virtual Task PostConfigureServicesAsync(ServiceConfigurationContext context)
    {
        PostConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 服务注册后段: 在所有模块的 <see cref="ConfigureServices"/> 之后执行。
    /// <para>适合放"要看到依赖模块登记了什么才能决定"的登记(替换默认实现、按已有登记补装饰器)。
    /// 约定登记在这之后统一写入, 所以这里的显式登记同样压得住约定登记。</para>
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void PostConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 应用预初始化阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    public virtual Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnPreApplicationInitialization(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 应用预初始化阶段: 容器已构建, HTTP 管道骨架还没展开。
    /// <para>在这里写 <c>context.GetApplicationBuilder().UseXxx()</c>, 中间件同样落在骨架之外(最外层),
    /// 与 <see cref="OnApplicationInitialization"/> 的位置相同; 差别只在于它比所有模块的第一段都早跑完。</para>
    /// <para>适合放"必须最早生效"的一次性接线(对象访问器填值、平台级探针)。</para>
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnPreApplicationInitialization(ApplicationInitializationContext context) { }

    /// <summary>
    /// 应用初始化第一段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    public virtual Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnApplicationInitialization(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 应用初始化第一段: 适合订阅事件、预热缓存等需要容器的动作。
    /// <para>要往 HTTP 管道里加中间件也可以在这里写: <c>context.GetApplicationBuilder().UseXxx()</c>。
    /// 这时骨架还没展开, 所以落点在最外层 —— 链路标识、路由、认证、授权都被它包在里面。</para>
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnApplicationInitialization(ApplicationInitializationContext context) { }

    /// <summary>
    /// 应用初始化收尾段的异步版本: 默认调用同步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    public virtual Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnPostApplicationInitialization(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 应用初始化收尾段: 在全部模块跑完第一段、HTTP 管道骨架展开之后、端点映射之前执行。
    /// <para>这里写 <c>context.GetApplicationBuilder().UseXxx()</c>, 中间件落在"授权之后、端点之前",
    /// 适合放要读已认证主体的东西(限流)。</para>
    /// <para>同一阶段内的先后 = 模块依赖顺序(<see cref="DependsOnAttribute"/> 拓扑序); 需要固定先后就给模块加依赖。</para>
    /// <para>非 Web 宿主没有管道可写, 这一段照样会跑。</para>
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnPostApplicationInitialization(ApplicationInitializationContext context) { }

    /// <summary>
    /// 应用关停阶段的异步版本: 默认调用同步版本
    /// </summary>
    /// <param name="context">应用关停上下文</param>
    /// <returns>表示关停过程的任务</returns>
    public virtual Task OnApplicationShutdownAsync(ApplicationShutdownContext context)
    {
        OnApplicationShutdown(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 应用关停阶段: Web 宿主在 <c>IHostApplicationLifetime.ApplicationStopping</c> 时触发, 非 Web 宿主调
    /// <c>IServiceProvider.Shutdown()</c> 触发。
    /// <para>适合冲刷缓冲、注销注册、停止自建的后台循环。容器还没释放, 服务仍然可用。</para>
    /// </summary>
    /// <param name="context">应用关停上下文</param>
    public virtual void OnApplicationShutdown(ApplicationShutdownContext context) { }

    /// <summary>
    /// 按配置节绑定选项(对应 ABP 的 AbpModule.Configure)。
    /// <para>节名直接写字面量,不另设常量,与 ABP 一致。</para>
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="configuration">配置(通常是 context.Configuration.GetSection("Xxx"))</param>
    protected void Configure<TOptions>(IConfiguration configuration) where TOptions : class
    {
        ServiceConfigurationContext.Services.Configure<TOptions>(configuration);
    }

    /// <summary>
    /// 用代码给选项赋值(对应 ABP 的 AbpModule.Configure)。
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="configureOptions">配置动作</param>
    protected void Configure<TOptions>(Action<TOptions> configureOptions) where TOptions : class
    {
        ServiceConfigurationContext.Services.Configure(configureOptions);
    }
}
