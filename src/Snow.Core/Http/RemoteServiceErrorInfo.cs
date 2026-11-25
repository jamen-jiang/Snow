using System.Collections;

namespace Snow.Http;

/// <summary>
/// 远端错误信息(对应 ABP 的 <c>RemoteServiceErrorInfo</c>)
/// </summary>
[Serializable]
public class RemoteServiceErrorInfo
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
    /// 细节(堆栈等), 是否返回给客户端由异常处理选项控制
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 附加数据, 原样序列化给客户端
    /// </summary>
    public IDictionary? Data { get; set; }

    /// <summary>
    /// 字段级校验错误(对应 ABP 的 <c>RemoteServiceValidationErrorInfo[]</c>)
    /// </summary>
    public RemoteServiceValidationErrorInfo[]? ValidationErrors { get; set; }

    /// <summary>
    /// 创建一个空实例
    /// </summary>
    public RemoteServiceErrorInfo()
    {
    }

    /// <summary>
    /// 用错误消息创建一个实例(与 ABP 的 <c>RemoteServiceErrorInfo(string message, ...)</c> 对齐)
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="details">错误细节</param>
    /// <param name="code">错误码</param>
    /// <param name="data">附加数据</param>
    public RemoteServiceErrorInfo(
        string message,
        string? details = null,
        string? code = null,
        IDictionary? data = null)
    {
        Message = message;
        Details = details;
        Code = code;
        Data = data;
    }
}
