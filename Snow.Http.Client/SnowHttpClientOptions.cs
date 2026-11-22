namespace Snow.Http.Client;

/// <summary>
/// HTTP 客户端选项(配置节 <c>HttpClient</c>): 顶层值是所有客户端的默认值,
/// 具名节 <c>HttpClient:{名字}</c> 覆盖个别客户端(名字规则见 <see cref="SnowHttpClientNames.For(Type)"/>)
/// <para>业务自己的类型化客户端照常写 <c>services.AddHttpClient&lt;TClient&gt;(...)</c>, 不受这里影响。</para>
/// </summary>
public class SnowHttpClientOptions
{
    private static readonly string[] OptionKeys = ["BaseAddress", "Timeout", "UserAgent", "DefaultHeaders"];

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

    /// <summary>
    /// 判断配置节下的某个键是否本选项自身的属性(用于区分「客户端名」与「选项键」)
    /// </summary>
    /// <param name="key">配置节子键</param>
    /// <returns>是选项键返回 true</returns>
    internal static bool IsOptionKey(string key)
    {
        return Array.Exists(OptionKeys, optionKey => string.Equals(optionKey, key, StringComparison.OrdinalIgnoreCase));
    }
}
