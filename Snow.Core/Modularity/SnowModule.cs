using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块基类(对应 ABP 的 AbpModule)。一个模块代表一个可复用的功能单元(通常对应一个程序集),
/// 通过重写各阶段的钩子来注册服务与配置中间件。
/// 加载顺序由 <see cref="DependsOnAttribute"/> 描述,加载器负责拓扑排序。
/// <para>阶段顺序: PreConfigureServices → ConfigureServices → PostConfigureServices →
/// OnPreApplicationInitialization → OnApplicationInitialization → OnPostApplicationInitialization →
/// OnApplicationShutdown。</para>
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
    /// 当前的服务配置上下文,只在服务注册的三个阶段内可用
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
                    nameof(ServiceConfigurationContext) + " 只在 " + nameof(PreConfigureServices) + "、" +
                    nameof(ConfigureServices) + " 与 " + nameof(PostConfigureServices) + " 阶段可用。");
            }

            return _serviceConfigurationContext;
        }

        internal set => _serviceConfigurationContext = value;
    }

    /// <summary>
    /// 前置注册阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    public virtual Task PreConfigureServicesAsync(ServiceConfigurationContext context)
    {
        PreConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 前置注册阶段:在全部模块的 ConfigureServices 之前执行
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
    /// 服务注册阶段:约定扫描本模块程序集之后调用,
    /// 因此这里显式登记的服务会优先于约定注册生效。
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void ConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 后置注册阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    public virtual Task PostConfigureServicesAsync(ServiceConfigurationContext context)
    {
        PostConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 后置注册阶段:在全部模块的 ConfigureServices 之后执行,适合覆盖式注册
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public virtual void PostConfigureServices(ServiceConfigurationContext context) { }

    /// <summary>
    /// 前置初始化阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    public virtual Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnPreApplicationInitialization(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 前置初始化阶段:在全部模块的 OnApplicationInitialization 之前执行
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
    /// 后置初始化阶段的异步版本:默认调用同步版本
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    /// <returns>表示初始化过程的任务</returns>
    public virtual Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnPostApplicationInitialization(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 后置初始化阶段:在全部模块的 OnApplicationInitialization 之后执行
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

    /// <summary>
    /// 按配置节绑定选项:模块内绑定配置的统一入口(对应 ABP 的 AbpModule.Configure)。
    /// <para>节名直接写字面量,不另设常量,与 ABP 一致。</para>
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="configuration">配置(通常是 context.Services.GetConfiguration().GetSection("Xxx"))</param>
    protected void Configure<TOptions>(IConfiguration configuration) where TOptions : class
    {
        ServiceConfigurationContext.Services.Configure<TOptions>(configuration);
    }

    /// <summary>
    /// 按配置节绑定选项,并指定绑定器行为(对应 ABP 的 AbpModule.Configure)。
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="configuration">配置节</param>
    /// <param name="configureBinder">绑定器行为</param>
    protected void Configure<TOptions>(IConfiguration configuration, Action<BinderOptions> configureBinder) where TOptions : class
    {
        ServiceConfigurationContext.Services.Configure<TOptions>(configuration, configureBinder);
    }

    /// <summary>
    /// 用具名选项绑定配置节(对应 ABP 的 AbpModule.Configure)。
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="name">选项名</param>
    /// <param name="configuration">配置节</param>
    protected void Configure<TOptions>(string name, IConfiguration configuration) where TOptions : class
    {
        ServiceConfigurationContext.Services.Configure<TOptions>(name, configuration);
    }

    /// <summary>
    /// 用代码给选项赋默认值(对应 ABP 的 AbpModule.Configure)。
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="configureOptions">配置动作</param>
    protected void Configure<TOptions>(Action<TOptions> configureOptions) where TOptions : class
    {
        ServiceConfigurationContext.Services.Configure(configureOptions);
    }

    /// <summary>
    /// 用具名选项执行代码配置(对应 ABP 的 AbpModule.Configure)。
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="name">选项名</param>
    /// <param name="configureOptions">配置动作</param>
    protected void Configure<TOptions>(string name, Action<TOptions> configureOptions) where TOptions : class
    {
        ServiceConfigurationContext.Services.Configure(name, configureOptions);
    }

    /// <summary>
    /// 在全部 Configure 之后执行(对应 ABP 的 AbpModule.PostConfigure)。
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="configureOptions">配置动作</param>
    protected void PostConfigure<TOptions>(Action<TOptions> configureOptions) where TOptions : class
    {
        ServiceConfigurationContext.Services.PostConfigure(configureOptions);
    }

    /// <summary>
    /// 在全部 PostConfigure 之后执行(对应 ABP 的 AbpModule.PostConfigureAll)。
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="configureOptions">配置动作</param>
    protected void PostConfigureAll<TOptions>(Action<TOptions> configureOptions) where TOptions : class
    {
        ServiceConfigurationContext.Services.PostConfigureAll(configureOptions);
    }

    /// <summary>
    /// 判断类型是否是可实例化的模块类型(对应 ABP 的 AbpModule.IsAbpModule)
    /// </summary>
    /// <param name="type">待判断的类型</param>
    /// <returns>是模块类型时返回 true</returns>
    public static bool IsSnowModule(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsClass && !type.IsAbstract && !type.IsGenericType && typeof(SnowModule).IsAssignableFrom(type);
    }
}
