using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Snow.Http.Client;

/// <summary>
/// 按客户端名字解析生效选项: 以 <c>HttpClient</c> 节(全局默认值)为底, 再用 <c>HttpClient:{名字}</c> 节覆盖
/// </summary>
public sealed class SnowHttpClientCatalog
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<SnowHttpClientOptions> _defaults;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="configuration">配置根</param>
    /// <param name="defaults">全局默认选项</param>
    public SnowHttpClientCatalog(IConfiguration configuration, IOptionsMonitor<SnowHttpClientOptions> defaults)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(defaults);

        _configuration = configuration;
        _defaults = defaults;
    }

    /// <summary>
    /// 解析某契约类型生效的选项
    /// </summary>
    /// <param name="contractType">客户端契约(接口)类型</param>
    /// <returns>生效选项</returns>
    public SnowHttpClientOptions Resolve(Type contractType)
    {
        ArgumentNullException.ThrowIfNull(contractType);

        return Resolve(SnowHttpClientNames.For(contractType));
    }

    /// <summary>
    /// 解析某名字生效的选项
    /// </summary>
    /// <param name="name">客户端名称</param>
    /// <returns>生效选项</returns>
    public SnowHttpClientOptions Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        SnowHttpClientOptions options = _defaults.CurrentValue.Clone();

        IConfigurationSection section = _configuration.GetSection("HttpClient:" + name);
        if (section.Exists())
        {
            section.Bind(options);
        }

        return options;
    }

    /// <summary>
    /// 该名字是否已有自己的配置节(有则命名客户端管线已按同一份选项配好, 调用方不必再应用一次, 避免重复追加请求头)
    /// </summary>
    /// <param name="name">客户端名称</param>
    /// <returns>已配好返回 true</returns>
    public bool IsPreconfigured(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return string.Equals(name, SnowHttpClientNames.Default, StringComparison.Ordinal)
            || _configuration.GetSection("HttpClient:" + name).Exists();
    }
}
