using System.Net;

namespace Snow.Http.Client;

/// <summary>
/// 声明式 HTTP 接口调用失败(非 2xx 响应, 或契约定义错误)
/// </summary>
public sealed class SnowHttpServiceException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="method">HTTP 动词</param>
    /// <param name="url">请求地址</param>
    /// <param name="statusCode">响应状态码, 契约定义错误时为 null</param>
    /// <param name="responseBody">响应体(截断)</param>
    /// <param name="innerException">内部异常</param>
    public SnowHttpServiceException(
        string message,
        HttpMethod method,
        string url,
        HttpStatusCode? statusCode = null,
        string? responseBody = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Method = method;
        Url = url;
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// HTTP 动词
    /// </summary>
    public HttpMethod Method { get; }

    /// <summary>
    /// 请求地址
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// 响应体(最多 2000 字符)
    /// </summary>
    public string? ResponseBody { get; }
}
