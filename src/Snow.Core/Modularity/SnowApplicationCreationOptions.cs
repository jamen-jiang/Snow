using Microsoft.Extensions.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 应用创建选项(对应 ABP 的 <c>AbpApplicationCreationOptions</c>)
/// <para>ABP 那份还有 <c>PlugInSources</c> / <c>Configuration</c> / <c>SkipConfigureServices</c> / <c>ApplicationName</c> 四项,
/// 分别服务于插件源、自建配置、跳过服务注册、应用信息访问器; 这几个子系统 Snow 目前都没有,
/// 所以这里只留真正被消费的两项 —— 少一个成员比多一个没人读的成员好。等那些子系统落地再往这里加。</para>
/// </summary>
public class SnowApplicationCreationOptions
{
    /// <summary>
    /// 宿主使用的服务集合
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 环境名, 写进 <see cref="ISnowHostEnvironment"/>
    /// <para>Web 宿主不用自己填: <c>WebApplicationBuilderExtensions.AddApplicationAsync()</c> 会从
    /// <c>builder.Environment.EnvironmentName</c> 抄过来, 与 ABP 的做法一致。</para>
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="services">宿主使用的服务集合</param>
    public SnowApplicationCreationOptions(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Services = services;
    }
}
