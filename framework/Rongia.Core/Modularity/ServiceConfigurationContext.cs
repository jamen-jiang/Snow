using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 服务配置上下文:在模块的服务注册钩子之间传递服务集合与模块清单
/// </summary>
public sealed class ServiceConfigurationContext
{
    /// <summary>
    /// 服务集合,与宿主使用的 IServiceCollection 是同一个实例
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 本次应用加载的全部模块所在程序集,按模块加载顺序去重。
    /// 模块需要"扫自己相关的程序集"时直接用它,不必依赖宿主额外传参
    /// </summary>
    public IReadOnlyList<Assembly> ModuleAssemblies { get; }

    /// <summary>
    /// 跨模块共享的状态袋
    /// </summary>
    public IDictionary<string, object?> Items { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="moduleAssemblies">模块所在程序集清单</param>
    public ServiceConfigurationContext(IServiceCollection services, IReadOnlyList<Assembly> moduleAssemblies)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        ModuleAssemblies = moduleAssemblies ?? Array.Empty<Assembly>();
        Items = new Dictionary<string, object?>();
    }
}
