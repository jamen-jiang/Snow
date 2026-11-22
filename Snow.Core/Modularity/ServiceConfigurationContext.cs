using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 服务配置上下文:在模块的服务注册钩子之间传递服务集合与配置
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
    /// 构造器
    /// </summary>
    /// <param name="services">服务集合</param>
    public ServiceConfigurationContext(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
}
