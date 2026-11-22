using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 模块基类(对应 ABP 的 AbpModule)。一个模块代表一个可复用的功能单元(通常对应一个程序集),
/// 通过重写 <see cref="ConfigureServices"/> 登记服务, 重写 <see cref="OnApplicationInitialization"/> 做需要容器的启动期动作。
/// 模块不碰 HTTP 管道: 中间件由宿主在自己的启动模块里按行序写 <c>app.UseXxx()</c>, 框架既不替你决定顺序, 也不替你挂任何一层;
/// 端点由模块用 <c>Configure&lt;SnowEndpointRouterOptions&gt;</c> 声明, 由框架在全部模块初始化完成之后统一映射(位置唯一, 不需要顺序信息)。
/// 加载顺序由 <see cref="DependsOnAttribute"/> 描述,加载器负责拓扑排序。
/// <para>阶段顺序: ConfigureServices → OnApplicationInitialization。</para>
/// </summary>
public abstract class SnowModule
{
    private ServiceConfigurationContext? _serviceConfigurationContext;

    /// <summary>
    /// 为 true 时本模块所在程序集不做约定扫描,服务全部由模块自己显式登记
    /// </summary>
    protected internal bool SkipAutoServiceRegistration { get; protected set; }

    /// <summary>
    /// 当前的服务配置上下文,只在 <see cref="ConfigureServices"/> 阶段内可用
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
    /// 应用初始化阶段:适合订阅事件、预热缓存等需要容器的动作。
    /// <para>管道不在这里写: 它由 <c>SnowAspNetCoreModule</c> 在本阶段按固定骨架展开, 各包的中间件位置在
    /// <see cref="ConfigureServices"/> 里就声明好了。宿主启动模块通常什么都不用重写。</para>
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public virtual void OnApplicationInitialization(ApplicationInitializationContext context) { }

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