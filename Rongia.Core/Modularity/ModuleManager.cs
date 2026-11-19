using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Rongia.Modularity;

/// <summary>
/// 模块生命周期管理器的默认实现(对应 ABP 的 ModuleManager):
/// 先按贡献者顺序、再按模块顺序双层遍历; 关闭时模块顺序取逆序
/// </summary>
public class ModuleManager : IModuleManager
{
    private readonly IModuleContainer _moduleContainer;
    private readonly IModuleLifecycleContributor[] _lifecycleContributors;
    private readonly ILogger<ModuleManager>? _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="moduleContainer">模块容器</param>
    /// <param name="options">生命周期配置,提供贡献者顺序</param>
    /// <param name="serviceProvider">服务提供者,用于解析贡献者实例</param>
    public ModuleManager(
        IModuleContainer moduleContainer,
        IOptions<RongiaModuleLifecycleOptions> options,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(moduleContainer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _moduleContainer = moduleContainer;

        // 日志是可选的: 宿主没有注册日志基础设施时照常工作
        _logger = serviceProvider.GetService<ILogger<ModuleManager>>();

        _lifecycleContributors = options.Value.Contributors
            .Select(serviceProvider.GetRequiredService)
            .Cast<IModuleLifecycleContributor>()
            .ToArray();
    }

    /// <inheritdoc />
    public virtual async Task InitializeModulesAsync(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IModuleLifecycleContributor contributor in _lifecycleContributors)
        {
            foreach (RongiaModuleDescriptor module in _moduleContainer.Modules)
            {
                try
                {
                    await contributor.InitializeAsync(context, module.Instance);
                }
                catch (Exception exception)
                {
                    throw new RongiaInitializationException(
                        "模块 " + module.Type.AssemblyQualifiedName + " 在初始化阶段 " + contributor.GetType().FullName + " 抛出异常: " + exception.Message,
                        exception);
                }
            }
        }

        _logger?.LogInformation("全部模块初始化完成");
    }

    /// <inheritdoc />
    public virtual void InitializeModules(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IModuleLifecycleContributor contributor in _lifecycleContributors)
        {
            foreach (RongiaModuleDescriptor module in _moduleContainer.Modules)
            {
                try
                {
                    contributor.Initialize(context, module.Instance);
                }
                catch (Exception exception)
                {
                    throw new RongiaInitializationException(
                        "模块 " + module.Type.AssemblyQualifiedName + " 在初始化阶段 " + contributor.GetType().FullName + " 抛出异常: " + exception.Message,
                        exception);
                }
            }
        }

        _logger?.LogInformation("全部模块初始化完成");
    }

    /// <inheritdoc />
    public virtual async Task ShutdownModulesAsync(ApplicationShutdownContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<RongiaModuleDescriptor> modules = _moduleContainer.Modules.Reverse().ToList();

        foreach (IModuleLifecycleContributor contributor in _lifecycleContributors)
        {
            foreach (RongiaModuleDescriptor module in modules)
            {
                try
                {
                    await contributor.ShutdownAsync(context, module.Instance);
                }
                catch (Exception exception)
                {
                    throw new RongiaShutdownException(
                        "模块 " + module.Type.AssemblyQualifiedName + " 在关闭阶段 " + contributor.GetType().FullName + " 抛出异常: " + exception.Message,
                        exception);
                }
            }
        }

        _logger?.LogInformation("全部模块关闭完成");
    }

    /// <inheritdoc />
    public virtual void ShutdownModules(ApplicationShutdownContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<RongiaModuleDescriptor> modules = _moduleContainer.Modules.Reverse().ToList();

        foreach (IModuleLifecycleContributor contributor in _lifecycleContributors)
        {
            foreach (RongiaModuleDescriptor module in modules)
            {
                try
                {
                    contributor.Shutdown(context, module.Instance);
                }
                catch (Exception exception)
                {
                    throw new RongiaShutdownException(
                        "模块 " + module.Type.AssemblyQualifiedName + " 在关闭阶段 " + contributor.GetType().FullName + " 抛出异常: " + exception.Message,
                        exception);
                }
            }
        }

        _logger?.LogInformation("全部模块关闭完成");
    }
}
