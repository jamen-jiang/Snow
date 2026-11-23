namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 统一响应信封(对应 ABP 的 RemoteServiceResponse)
/// <para>成功时 <see cref="Result"/> 有值、<see cref="Error"/> 为 null; 失败时相反, HTTP 状态码仍表达大类。</para>
/// </summary>
public class SnowApiResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 成功时的返回值
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// 失败时的错误信息
    /// </summary>
    public SnowApiErrorInfo? Error { get; set; }

    /// <summary>
    /// 构造一个成功响应
    /// </summary>
    /// <param name="result">返回值</param>
    /// <returns>响应信封</returns>
    public static SnowApiResponse Ok(object? result) => new() { Success = true, Result = result };

    /// <summary>
    /// 构造一个失败响应
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="message">错误消息</param>
    /// <param name="details">附加数据</param>
    /// <param name="validationErrors">字段校验错误</param>
    /// <returns>响应信封</returns>
    public static SnowApiResponse Fail(
        string code,
        string message,
        string? details = null,
        IDictionary<string, string[]>? validationErrors = null)
        => new()
        {
            Success = false,
            Error = new SnowApiErrorInfo
            {
                Code = code,
                Message = message,
                Details = details,
                ValidationErrors = validationErrors,
            },
        };
}

/// <summary>
/// 错误信息(对应 ABP 的 RemoteServiceErrorInfo)
/// </summary>
public class SnowApiErrorInfo
{
    /// <summary>
    /// 错误码, 客户端据此分支处理
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 附加数据(堆栈等), 由 <see cref="SnowMvcOptions.IncludeExceptionMessage"/> 控制是否填充
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 字段级校验错误: 字段名 → 错误消息列表
    /// </summary>
    public IDictionary<string, string[]>? ValidationErrors { get; set; }
}
