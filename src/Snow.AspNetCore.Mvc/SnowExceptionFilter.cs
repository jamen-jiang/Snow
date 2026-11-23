using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow;
using Snow.AspNetCore.Filters;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 把异常翻译成统一错误响应与合适的 HTTP 状态码的过滤器
/// <para>业务异常(<see cref="SnowUserFriendlyException"/>)按消息返回; 其余异常默认只回"服务器内部错误", 细节靠日志。</para>
/// <para>实现 <see cref="ISnowFilter"/>: 控制器标了 <see cref="DisableSnowFeaturesAttribute"/> 时由
/// <c>SnowMvcActionDescriptorProvider</c> 在动作描述符定型时把它整个摘掉, 请求期不做判断。</para>
/// </summary>
public sealed class SnowExceptionFilter : IExceptionFilter, ISnowFilter
{
    private readonly SnowMvcOptions _options;
    private readonly ILogger<SnowExceptionFilter> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">MVC 选项</param>
    /// <param name="logger">日志</param>
    public SnowExceptionFilter(IOptions<SnowMvcOptions> options, ILogger<SnowExceptionFilter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Exception exception = context.Exception;
        (int statusCode, string code, string message) = Translate(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "请求处理失败: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("请求被拒绝({StatusCode}): {Message}", statusCode, exception.Message);
        }

        string? details = _options.IncludeExceptionMessage ? exception.ToString() : null;

        context.Result = new ObjectResult(SnowApiResponse.Fail(code, message, details))
        {
            StatusCode = statusCode,
        };
        context.ExceptionHandled = true;
    }

    private (int StatusCode, string Code, string Message) Translate(Exception exception)
    {
        switch (exception)
        {
            case SnowUserFriendlyException friendly:
                return (StatusCodes.Status400BadRequest, friendly.Code ?? "BusinessError", friendly.Message);

            case UnauthorizedAccessException:
                return (StatusCodes.Status403Forbidden, "Forbidden", "没有访问该资源的权限");

            case KeyNotFoundException:
                return (StatusCodes.Status404NotFound, "NotFound", exception.Message);

            case NotImplementedException:
                return (StatusCodes.Status501NotImplemented, "NotImplemented", "该功能尚未实现");

            case ArgumentException or InvalidOperationException:
                return (StatusCodes.Status400BadRequest, "InvalidArgument", exception.Message);

            default:
                return (StatusCodes.Status500InternalServerError, "InternalError",
                    _options.IncludeExceptionMessage ? exception.Message : "服务器内部错误");
        }
    }
}
