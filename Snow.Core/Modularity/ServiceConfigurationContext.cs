using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 服务配置上下文:在模块的服务注册钩子之间传递服务集合与共享状态。
/// 成员与 ABP 的 ServiceConfigurationContext 对齐(Services / Configuration / Items / 索引器)
/// </summary>
public sealed class ServiceConfigurationContext
{
    private IConfiguration? _configuration;

    /// <summary>
    /// 服务集合,与宿主使用的 IServiceCollection 是同一个实例
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 宿主已注册的配置对象,首次访问时从服务集合中取
    /// </summary>
    public IConfiguration Configuration => _configuration ??= Services.GetConfiguration();

    /// <summary>
    /// 跨模块共享的状态袋
    /// </summary>
    public IDictionary<string, object?> Items { get; }

    /// <summary>
    /// 按名称读写共享状态,等价于 Items 的便捷访问
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>存在时返回对应的值,否则返回 null</returns>
    public object? this[string key]
    {
        get => Items.TryGetValue(key, out object? value) ? value : null;
        set => Items[key] = value;
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="services">服务集合</param>
    public ServiceConfigurationContext(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Items = new Dictionary<string, object?>();
    }
}
