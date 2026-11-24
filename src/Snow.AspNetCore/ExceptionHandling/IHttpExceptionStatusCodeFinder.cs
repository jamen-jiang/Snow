using System;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// HTTP 状态码解析器(对应 ABP 的 <c>IHttpExceptionStatusCodeFinder</c>): 由异常与请求上下文决定响应的状态码。
/// </summary>
public interface IHttpExceptionStatusCodeFinder
{
    /// <summary>
    /// 解析状态码
    /// </summary>
    /// <param name="httpContext">当前 HTTP 上下文</param>
    /// <param name="exception">异常</param>
    HttpStatusCode GetStatusCode(HttpContext httpContext, Exception exception);
}
