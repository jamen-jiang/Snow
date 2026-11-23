namespace Snow.Http.Client;

/// <summary>
/// <see cref="ISnowHttpClient{T}"/> 的默认实现(瞬态, 每次注入一个新的 <see cref="HttpClient"/>)
/// </summary>
/// <typeparam name="T">客户端契约(接口)类型</typeparam>
internal sealed class SnowHttpClient<T> : ISnowHttpClient<T>, IDisposable where T : class
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="httpClientFactory">客户端工厂</param>
    /// <param name="catalog">选项目录</param>
    public SnowHttpClient(IHttpClientFactory httpClientFactory, SnowHttpClientCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(catalog);

        Name = SnowHttpClientNames.For<T>();
        Options = catalog.Resolve(Name);
        HttpClient = httpClientFactory.CreateClient(Name);

        // 配置节里出现过的名字, 模块已为它建好管线并应用过同一份选项; 再应用一次会重复追加 UserAgent 与请求头
        if (!catalog.IsPreconfigured(Name))
        {
            SnowHttpClientOptionApplier.Apply(HttpClient, Options);
        }
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public HttpClient HttpClient { get; }

    /// <inheritdoc />
    public SnowHttpClientOptions Options { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        HttpClient.Dispose();
    }
}
