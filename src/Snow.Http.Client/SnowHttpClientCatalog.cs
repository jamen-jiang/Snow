using Microsoft.Extensions.Options;

namespace Snow.Http.Client;

/// <summary>
/// 按客户端名字解析生效选项: 以 <see cref="SnowHttpClientOptions"/> 的顶层值为底,
/// 若 <see cref="SnowHttpClientOptions.Clients"/> 里有同名项则整份用它
/// </summary>
public sealed class SnowHttpClientCatalog
{
    private readonly IOptionsMonitor<SnowHttpClientOptions> _defaults;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="defaults">全局默认选项</param>
    public SnowHttpClientCatalog(IOptionsMonitor<SnowHttpClientOptions> defaults)
    {
        ArgumentNullException.ThrowIfNull(defaults);

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

        SnowHttpClientOptions defaults = _defaults.CurrentValue;

        return defaults.Clients.TryGetValue(name, out SnowHttpClientOptions? named) ? named.Clone() : defaults.Clone();
    }

    /// <summary>
    /// 该名字的管线是否已由模块建好并应用过同一份选项 —— 只有默认客户端是这种情况,
    /// 按契约派生的具名客户端由 <see cref="SnowHttpClient{T}"/> 自己应用, 不必再建一次管线
    /// </summary>
    /// <param name="name">客户端名称</param>
    /// <returns>已配好返回 true</returns>
    public bool IsPreconfigured(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return string.Equals(name, SnowHttpClientNames.Default, StringComparison.Ordinal);
    }
}
