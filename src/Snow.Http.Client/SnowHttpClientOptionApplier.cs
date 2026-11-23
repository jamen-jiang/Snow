namespace Snow.Http.Client;

/// <summary>
/// 把 <see cref="SnowHttpClientOptions"/> 应用到 <see cref="HttpClient"/> 上(模块的默认客户端与类型化客户端共用)
/// </summary>
internal static class SnowHttpClientOptionApplier
{
    /// <summary>
    /// 应用选项: 超时、基础地址、UserAgent 与默认请求头
    /// </summary>
    /// <param name="client">客户端</param>
    /// <param name="options">选项</param>
    internal static void Apply(HttpClient client, SnowHttpClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        client.Timeout = options.Timeout;

        if (!string.IsNullOrWhiteSpace(options.BaseAddress))
        {
            client.BaseAddress = new Uri(options.BaseAddress, UriKind.Absolute);
        }

        if (!string.IsNullOrWhiteSpace(options.UserAgent))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        }

        foreach (KeyValuePair<string, string> header in options.DefaultHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
    }
}
