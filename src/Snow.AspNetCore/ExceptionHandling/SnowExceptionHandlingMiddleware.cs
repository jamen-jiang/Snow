using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Snow.Exceptions;
using Snow.Logging;
using Snow.Validation;

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 异常处理中间件(对应 ABP 的 <c>AbpExceptionHandlingMiddleware</c>)
/// <para>把后续管道抛出的异常交给 <see cref="IExceptionToErrorInfoConverter"/> 翻译, 以 JSON 错误信封写回;
/// 响应已经开始写出时无法改写, 记日志后原样抛出。</para>
/// <para>日志级别取 <see cref="IHasLogLevel"/>(业务异常 Warning, 其余默认 Error), 未实现该接口的按状态码判定。</para>
/// <para>挂载位置: 尽量靠前(在认证之前), 才能兜住整条管道。</para>
/// </summary>
public sealed class SnowExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SnowExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="logger">日志</param>
    public SnowExceptionHandlingMiddleware(RequestDelegate next, ILogger<SnowExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 执行中间件
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="converter">异常转换器</param>
    public async Task InvokeAsync(HttpContext context, IExceptionToErrorInfoConverter converter)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(converter);

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                TryLog(exception, null, "响应已开始写出, 无法再转换成错误响应");
                throw;
            }

            SnowRemoteServiceErrorInfo error = converter.Convert(exception);
            TryLog(exception, error, "请求处理异常: {Code} {Message}");

            context.Response.Clear();
            context.Response.StatusCode = GetStatusCode(exception);
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new SnowRemoteServiceErrorResponse(error), context.RequestAborted);
        }
    }

    /// <summary>
    /// 记日志, 但日志系统本身出问题(比如没权限写 Windows 事件日志)不能影响错误响应
    /// </summary>
    private void TryLog(Exception exception, SnowRemoteServiceErrorInfo? error, string template)
    {
        try
        {
            LogLevel level = ResolveLogLevel(exception);

            if (error is null)
            {
                _logger.Log(level, exception, template);
                return;
            }

            _logger.Log(level, exception, template, error.Code, error.Message);
        }
        catch (Exception loggingFailure)
        {
            // 写日志失败也只吞掉, 响应才是主路径
            _ = loggingFailure;
        }
    }

    private static LogLevel ResolveLogLevel(Exception exception) => exception switch
    {
        IHasLogLevel hasLogLevel => hasLogLevel.LogLevel,
        _ => LogLevel.Error,
    };

    private static int GetStatusCode(Exception exception) => exception switch
    {
        SnowValidationException => StatusCodes.Status400BadRequest,
        SnowBusinessException => StatusCodes.Status400BadRequest,
        UnauthorizedAccessException => StatusCodes.Status403Forbidden,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        NotImplementedException => StatusCodes.Status501NotImplemented,
        ArgumentException or InvalidOperationException => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError,
    };
}