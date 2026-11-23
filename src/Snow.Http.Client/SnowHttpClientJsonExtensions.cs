using System.Net.Http.Json;

namespace Snow.Http.Client;

/// <summary>
/// <see cref="ISnowHttpClient{T}"/> 的便捷 JSON 方法: 一行发请求并反序列化, 非 2xx 抛 <see cref="SnowHttpServiceException"/>
/// </summary>
public static class SnowHttpClientJsonExtensions
{
    /// <summary>
    /// GET 并反序列化
    /// </summary>
    /// <typeparam name="T">契约(决定客户端名称与配置节)</typeparam>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="client">客户端访问器</param>
    /// <param name="path">相对路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应对象</returns>
    public static async Task<TResponse> GetJsonAsync<T, TResponse>(this ISnowHttpClient<T> client, string path, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using HttpResponseMessage response = await client.HttpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
        return await SnowHttpClientJsonExtensions.ReadAsync<TResponse>(response, HttpMethod.Get, path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// POST 并反序列化
    /// </summary>
    /// <typeparam name="T">契约(决定客户端名称与配置节)</typeparam>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="client">客户端访问器</param>
    /// <param name="path">相对路径</param>
    /// <param name="body">请求体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应对象</returns>
    public static async Task<TResponse> PostJsonAsync<T, TResponse>(this ISnowHttpClient<T> client, string path, object? body, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using HttpResponseMessage response = await client.HttpClient.PostAsJsonAsync(path, body, SnowHttpServiceOptions.Default.JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        return await SnowHttpClientJsonExtensions.ReadAsync<TResponse>(response, HttpMethod.Post, path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// POST 不关心响应体
    /// </summary>
    /// <typeparam name="T">契约(决定客户端名称与配置节)</typeparam>
    /// <param name="client">客户端访问器</param>
    /// <param name="path">相对路径</param>
    /// <param name="body">请求体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public static async Task PostJsonAsync<T>(this ISnowHttpClient<T> client, string path, object? body, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using HttpResponseMessage response = await client.HttpClient.PostAsJsonAsync(path, body, SnowHttpServiceOptions.Default.JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        await SnowHttpClientJsonExtensions.EnsureSuccessAsync(response, HttpMethod.Post, path).ConfigureAwait(false);
    }

    private static async Task<TResponse> ReadAsync<TResponse>(HttpResponseMessage response, HttpMethod method, string path, CancellationToken cancellationToken)
    {
        await SnowHttpClientJsonExtensions.EnsureSuccessAsync(response, method, path).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return default!;
        }

        TResponse? result = await response.Content
            .ReadFromJsonAsync<TResponse>(SnowHttpServiceOptions.Default.JsonSerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return result!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, HttpMethod method, string path)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (body.Length > 2000)
        {
            body = body[..2000];
        }

        throw new SnowHttpServiceException(
            "HTTP 请求返回 " + (int)response.StatusCode + " " + response.StatusCode + ": " + method + " " + path,
            method,
            path,
            response.StatusCode,
            body);
    }
}
