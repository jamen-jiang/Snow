using Microsoft.Extensions.DependencyInjection;
using Rongia.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 模块化应用:持有模块容器与服务集合,
/// 负责按依赖顺序驱动模块的服务注册三阶段与初始化三阶段
/// </summary>
public sealed class Application
{
    /// <summary>
    /// 本次应用加载的模块容器
    /// </summary>
    public IModuleContainer Modules { get; }

    /// <summary>
    /// 服务集合,与宿主使用的 IServiceCollection 是同一个实例
    /// </summary>
    public IServiceCollection Services { get; }

    internal Application(IServiceCollection services, IModuleContainer modules)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
    }

    /// <summary>
    /// 驱动所有模块的服务注册阶段:
    /// 先跑全部模块的 PreConfigureServices,再逐模块做约定扫描与 ConfigureServices,
    /// 最后跑全部模块的 PostConfigureServices
    /// </summary>
    public void ConfigureServices()
    {
        IReadOnlyList<ModuleDescriptor> modules = Modules.Modules;

        ServiceConfigurationContext context = new ServiceConfigurationContext(
            Services,
            modules.Select(module => module.Type.Assembly).Distinct().ToArray());

        Services.AddSingleton(context);

        foreach (ModuleDescriptor module in modules)
        {
            module.Instance.PreConfigureServices(context);
        }

        foreach (ModuleDescriptor module in modules)
        {
            Services.AddConventionalRegistrars(module.Type.Assembly);
            module.Instance.ConfigureServices(context);
        }

        foreach (ModuleDescriptor module in modules)
        {
            module.Instance.PostConfigureServices(context);
        }
    }

    /// <summary>
    /// 驱动所有模块的应用初始化阶段,容器必须已经构建完成
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    public void Initialize(IServiceProvider serviceProvider)
    {
        ApplicationInitializationContext context = new ApplicationInitializationContext(serviceProvider);

        IReadOnlyList<ModuleDescriptor> modules = Modules.Modules;

        foreach (ModuleDescriptor module in modules)
        {
            module.Instance.OnPreApplicationInitialization(context);
        }

        foreach (ModuleDescriptor module in modules)
        {
            module.Instance.OnApplicationInitialization(context);
        }

        foreach (ModuleDescriptor module in modules)
        {
            module.Instance.OnPostApplicationInitialization(context);
        }
    }

    /// <summary>
    /// 驱动所有模块的关闭阶段,按加载顺序的逆序执行
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public void Shutdown(IServiceProvider serviceProvider)
    {
        ApplicationShutdownContext context = new ApplicationShutdownContext(serviceProvider);

        IReadOnlyList<ModuleDescriptor> modules = Modules.Modules;

        for (int index = modules.Count - 1; index >= 0; index--)
        {
            modules[index].Instance.OnApplicationShutdown(context);
        }
    }
}