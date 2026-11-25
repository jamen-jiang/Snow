namespace Snow.Http.Client;

/// <summary>
/// HTTP 客户端选项: <see cref="BaseAddress"/> / <see cref="Timeout"/> / <see cref="UserAgent"/> / <see cref="DefaultHeaders"/>
/// 是默认客户端与所有按契约派生的客户端的基准值, <see cref="Clients"/> 里的具名项覆盖个别客户端
/// (名字规则见 <see cref="SnowHttpClientNames.For(Type)"/>)
/// <para>模块不读配置节: 宿主在 <c>PreConfigureServices</c> 里 <c>Configure&lt;SnowHttpClientOptions&gt;(options => ...)</c>
/// 或 <c>Configure&lt;SnowHttpClientOptions&gt;(context.Configuration.GetSection("HttpClient"))</c> 给出。</para>
/// <para>业务自己的类型化客户端照常写 <c>services.AddHttpClient&lt;TClient&gt;(...)</c>, 不受这里影响。</para>
/// </summary>
public class SnowHttpClientOptions
{
    /// <summary>
    /// 基础地址(如 https://api.example.com)。相对路径请求会拼在它后面
    /// </summary>
    public string? BaseAddress { get; set; }

    /// <summary>
    /// 请求超时, 默认 30 秒
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// User-Agent 头, 不填则不带
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 默认请求头(每次请求都带), 键不区分大小写
    /// </summary>
    public IDictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 具名客户端的专属选项(键是客户端名, 如 <c>HkoTracking</c>), 整份替换默认值而不是叠加
    /// </summary>
    public IDictionary<string, SnowHttpClientOptions> Clients { get; } = new Dictionary<string, SnowHttpClientOptions>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 复制一份(默认请求头深拷贝), 供按契约派生独立选项时作为基准
    /// </summary>
    /// <returns>新的选项实例</returns>
    public SnowHttpClientOptions Clone()
    {
        SnowHttpClientOptions clone = new()
        {
            BaseAddress = BaseAddress,
            Timeout = Timeout,
            UserAgent = UserAgent,
        };

        foreach (KeyValuePair<string, string> header in DefaultHeaders)
        {
            clone.DefaultHeaders[header.Key] = header.Value;
        }

        return clone;
    }
}
