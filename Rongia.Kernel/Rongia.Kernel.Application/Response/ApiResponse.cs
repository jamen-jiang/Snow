using System.Text.Json.Serialization;
using Rongia.Kernel.Application.Enums;

namespace Rongia.Kernel.Application.Response;

/// <summary>
/// 统一响应包装
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponse<T>
{
    private string? _message;

    /// <summary>
    /// 状态码
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// 结果集(可空)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 信息;自定义为空时回退到状态枚举的标签
    /// </summary>
    public string Message
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_message))
            {
                var status = ApiStatusExtensions.FromCode(Code);
                return status == null ? "" : status.Value.GetDescription();
            }
            return _message;
        }
    }

    /// <summary>
    /// 默认构造器;状态码取 Success
    /// </summary>
    public ApiResponse() : this((int)ApiStatus.Success, null, default)
    {
    }

    /// <summary>
    /// 构造器(带数据)
    /// </summary>
    /// <param name="data">结果集(可空)</param>
    public ApiResponse(T? data) : this((int)ApiStatus.Success, null, data)
    {
    }

    /// <summary>
    /// 构造器(带信息)
    /// </summary>
    /// <param name="message">信息(可空)</param>
    public ApiResponse(string? message) : this((int)ApiStatus.Success, message, default)
    {
    }

    /// <summary>
    /// 构造器(状态枚举 + 信息)
    /// </summary>
    /// <param name="status">状态枚举</param>
    /// <param name="message">信息(可空)</param>
    public ApiResponse(ApiStatus status, string? message) : this((int)status, message, default)
    {
    }

    /// <summary>
    /// 构造器(状态码 + 信息)
    /// </summary>
    /// <param name="code">状态码</param>
    /// <param name="message">信息(可空)</param>
    public ApiResponse(int code, string? message) : this(code, message, default)
    {
    }

    private ApiResponse(int code, string? message, T? data)
    {
        Code = code;
        _message = message;
        Data = data;
    }

    /// <summary>
    /// 是否成功(不序列化进响应:code 已表达成败)
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Code == (int)ApiStatus.Success;

    // ---------- 静态工厂方法 ----------

    /// <summary>
    /// 成功响应(无数据)
    /// </summary>
    public static ApiResponse<T> Success() => new();

    /// <summary>
    /// 成功响应(带数据)
    /// </summary>
    /// <param name="data">数据(可空)</param>
    public static ApiResponse<T> Success(T? data) => new(data);

    /// <summary>
    /// 成功响应(带信息)
    /// </summary>
    /// <param name="message">信息(可空)</param>
    public static ApiResponse<T> SuccessMessage(string? message) => new(message);

    /// <summary>
    /// 失败响应(默认 Fail)
    /// </summary>
    /// <param name="message">信息(可空)</param>
    public static ApiResponse<T> Error(string? message) => new(ApiStatus.Fail, message);

    /// <summary>
    /// 失败响应(指定状态码)
    /// </summary>
    /// <param name="code">状态码</param>
    /// <param name="message">信息(可空)</param>
    public static ApiResponse<T> Error(int code, string? message) => new(code, message);

    /// <summary>
    /// 失败响应(指定枚举)
    /// </summary>
    /// <param name="status">状态枚举</param>
    /// <param name="message">信息(可空)</param>
    public static ApiResponse<T> Error(ApiStatus status, string? message) => new(status, message);
}
