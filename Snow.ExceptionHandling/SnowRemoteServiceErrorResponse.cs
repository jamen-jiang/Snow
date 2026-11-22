namespace Snow.ExceptionHandling;

/// <summary>
/// 远端错误响应信封(对应 ABP 的 RemoteServiceErrorResponse)
/// </summary>
public sealed class SnowRemoteServiceErrorResponse
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="error">错误信息</param>
    public SnowRemoteServiceErrorResponse(SnowRemoteServiceErrorInfo error) => Error = error;

    /// <summary>
    /// 错误信息
    /// </summary>
    public SnowRemoteServiceErrorInfo Error { get; }
}
