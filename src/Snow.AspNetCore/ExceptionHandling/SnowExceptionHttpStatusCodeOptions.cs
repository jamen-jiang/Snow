using System.Collections.Generic;
using System.Net;

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// HTTP 状态码选项(对应 ABP 的 <c>AbpExceptionHttpStatusCodeOptions</c>): 自定义错误码到状态码的映射。
/// </summary>
public class SnowExceptionHttpStatusCodeOptions
{
    /// <summary>
    /// 错误码 → 状态码 的映射表
    /// </summary>
    public IDictionary<string, HttpStatusCode> ErrorCodeToHttpStatusCodeMappings { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowExceptionHttpStatusCodeOptions()
    {
        ErrorCodeToHttpStatusCodeMappings = new Dictionary<string, HttpStatusCode>();
    }

    /// <summary>
    /// 登记一条错误码到状态码的映射
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <param name="httpStatusCode">状态码</param>
    public void Map(string errorCode, HttpStatusCode httpStatusCode)
    {
        ErrorCodeToHttpStatusCodeMappings[errorCode] = httpStatusCode;
    }
}
