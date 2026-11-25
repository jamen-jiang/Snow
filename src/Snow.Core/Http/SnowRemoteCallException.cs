using Snow.Exceptions;

namespace Snow.Http;

/// <summary>
/// 远程服务调用异常(对应 ABP 的 <c>AbpRemoteCallException</c>): 承载下游服务返回的结构化错误体。
/// <para>HTTP 客户端收到非 2xx 且响应体是 <see cref="SnowRemoteServiceErrorResponse"/> 信封时抛出本异常;
/// 服务端异常处理遇到它且 <see cref="Error"/> 不为空时, 直接透传原始错误体, 不再二次包装。</para>
/// </summary>
public class SnowRemoteCallException : SnowException, IHasErrorCode, IHasErrorDetails, IHasHttpStatusCode
{
    /// <summary>
    /// 远程响应的 HTTP 状态码
    /// </summary>
    public int HttpStatusCode { get; set; }

    /// <inheritdoc />
    public string? Code => Error?.Code;

    /// <inheritdoc />
    public string? Details => Error?.Details;

    /// <summary>
    /// 下游服务返回的原始错误信息
    /// </summary>
    public RemoteServiceErrorInfo? Error { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowRemoteCallException()
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="innerException">内部异常</param>
    public SnowRemoteCallException(string? message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="error">下游服务返回的原始错误信息</param>
    /// <param name="innerException">内部异常</param>
    public SnowRemoteCallException(RemoteServiceErrorInfo error, Exception? innerException = null)
        : base(error.Message, innerException)
    {
        Error = error;

        if (error.Data != null)
        {
            foreach (object? key in error.Data.Keys)
            {
                Data[key] = error.Data[key];
            }
        }
    }
}
