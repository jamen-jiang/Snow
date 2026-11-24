using Snow.Http.Client;

namespace Microsoft.Extensions.Http;

/// <summary>
/// <see cref="IHttpClientFactory"/> 的便捷扩展
/// </summary>
public static class SnowHttpClientFactoryExtensions
{
    /// <summary>
    /// 创建默认命名客户端(已按 <see cref="SnowHttpClientOptions"/> 配好基础地址、超时、UserAgent 与默认请求头)
    /// </summary>
    /// <param name="httpClientFactory">客户端工厂</param>
    /// <returns>默认命名客户端</returns>
    public static HttpClient CreateSnowClient(this IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        return httpClientFactory.CreateClient(SnowHttpClientNames.Default);
    }
}
