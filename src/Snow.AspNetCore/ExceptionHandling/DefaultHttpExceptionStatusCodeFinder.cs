using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Snow.Authorization;
using Snow.Data;
using Snow.DependencyInjection;
using Snow.Exceptions;
using Snow.Validation;

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 默认状态码解析器(对应 ABP 的 <c>DefaultHttpExceptionStatusCodeFinder</c>): 判定顺序与 ABP 一致。
/// </summary>
[TransientService]
public class DefaultHttpExceptionStatusCodeFinder : IHttpExceptionStatusCodeFinder
{
    /// <summary>
    /// 状态码选项
    /// </summary>
    protected SnowExceptionHttpStatusCodeOptions Options { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">状态码选项</param>
    public DefaultHttpExceptionStatusCodeFinder(IOptions<SnowExceptionHttpStatusCodeOptions> options)
    {
        Options = options.Value;
    }

    /// <inheritdoc />
    /// <param name="httpContext">HTTP 上下文</param>
    /// <param name="exception">异常</param>
    public virtual HttpStatusCode GetStatusCode(HttpContext httpContext, Exception exception)
    {
        // (1) 异常自带状态码
        if (exception is IHasHttpStatusCode hasHttpStatusCodeException && hasHttpStatusCodeException.HttpStatusCode > 0)
        {
            return (HttpStatusCode)hasHttpStatusCodeException.HttpStatusCode;
        }

        // (2) 错误码映射表
        if (exception is IHasErrorCode hasErrorCodeException
            && hasErrorCodeException.Code != null
            && Options.ErrorCodeToHttpStatusCodeMappings.TryGetValue(hasErrorCodeException.Code, out var statusCode))
        {
            return statusCode;
        }

        // (3) 授权异常: 已认证→403, 否则→401
        if (exception is SnowAuthorizationException)
        {
            return httpContext.User?.Identity?.IsAuthenticated == true
                ? HttpStatusCode.Forbidden
                : HttpStatusCode.Unauthorized;
        }

        // (4)-(9) 按异常类型兜底
        if (exception is SnowValidationException)
        {
            return HttpStatusCode.BadRequest;
        }

        if (exception is SnowDbConcurrencyException)
        {
            return HttpStatusCode.Conflict;
        }

        if (exception is NotImplementedException)
        {
            return HttpStatusCode.NotImplemented;
        }

        if (exception is IBusinessException)
        {
            return HttpStatusCode.Forbidden;
        }

        return HttpStatusCode.InternalServerError;
    }
}
