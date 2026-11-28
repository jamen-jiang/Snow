namespace Snow.Http;

/// <summary>
/// 远端错误响应信封(对应 ABP 的 <c>RemoteServiceErrorResponse</c>)
/// </summary>
public sealed class SnowRemoteServiceErrorResponse
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="error">错误信息</param>
    public SnowRemoteServiceErrorResponse(RemoteServiceErrorInfo error) => Error = error;

    /// <summary>
    /// 错误信息
    /// </summary>
    public RemoteServiceErrorInfo Error { get; }
}
