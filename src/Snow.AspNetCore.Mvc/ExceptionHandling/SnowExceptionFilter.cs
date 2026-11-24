using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Snow.AspNetCore.ExceptionHandling;
using Snow.AspNetCore.Filters;
using Snow.Authorization;
using Snow.DependencyInjection;
using Snow.ExceptionHandling;
using Snow.Http;
using Snow.Json;

namespace Snow.AspNetCore.Mvc.ExceptionHandling;

/// <summary>
/// 异常过滤器(对应 ABP 的 <c>AbpExceptionFilter</c>): 把控制器抛出的异常翻译成统一的远端错误响应。
/// <para>流程与 ABP 一致: <see cref="ShouldHandleException"/> 判定是否接管 → <see cref="HandleAndWrapException"/> 通知订阅者、
/// 设置错误格式响应头与状态码、把结果换成 <see cref="SnowRemoteServiceErrorResponse"/>; 授权异常交给授权异常处理器。</para>
/// </summary>
[TransientService]
public class SnowExceptionFilter : IAsyncExceptionFilter, ISnowFilter
{
    /// <inheritdoc />
    public virtual async Task OnExceptionAsync(ExceptionContext context)
    {
        if (!ShouldHandleException(context))
        {
            LogException(context, out _);
            return;
        }

        await HandleAndWrapException(context);
    }

    /// <summary>
    /// 判断本过滤器是否应接管这个异常
    /// </summary>
    /// <param name="context">异常上下文</param>
    protected virtual bool ShouldHandleException(ExceptionContext context)
    {
        if (context.ExceptionHandled)
        {
            return false;
        }

        if (context.ActionDescriptor.IsControllerAction() && context.ActionDescriptor.HasObjectResult())
        {
            return true;
        }

        if (context.HttpContext.Request.CanAccept(MimeTypes.Application.Json))
        {
            return true;
        }

        if (context.HttpContext.Request.IsAjax())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 通知订阅者并把异常包装成统一错误响应
    /// </summary>
    /// <param name="context">异常上下文</param>
    protected virtual async Task HandleAndWrapException(ExceptionContext context)
    {
        LogException(context, out var remoteServiceErrorInfo);

        await context.HttpContext.RequestServices
            .GetRequiredService<IExceptionNotifier>()
            .NotifyAsync(new ExceptionNotificationContext(context.Exception));

        if (context.Exception is SnowAuthorizationException)
        {
            await context.HttpContext.RequestServices
                .GetRequiredService<ISnowAuthorizationExceptionHandler>()
                .HandleAsync(context.Exception.As<SnowAuthorizationException>(), context.HttpContext);
        }
        else
        {
            if (!context.HttpContext.Response.HasStarted)
            {
                context.HttpContext.Response.Headers.Append(SnowHttpConsts.SnowErrorFormat, "true");
                context.HttpContext.Response.StatusCode = (int)context.HttpContext.RequestServices
                    .GetRequiredService<IHttpExceptionStatusCodeFinder>()
                    .GetStatusCode(context.HttpContext, context.Exception);
            }
            else
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<SnowExceptionFilter>>()
                    ?? NullLogger<SnowExceptionFilter>.Instance;
                logger.LogWarning("HTTP response has already started, cannot set headers and status code!");
            }

            context.Result = new ObjectResult(new SnowRemoteServiceErrorResponse(remoteServiceErrorInfo));
        }

        context.ExceptionHandled = true;
    }

    /// <summary>
    /// 转换异常并记录日志
    /// </summary>
    /// <param name="context">异常上下文</param>
    /// <param name="remoteServiceErrorInfo">转换出的远端错误信息</param>
    protected virtual void LogException(ExceptionContext context, out SnowRemoteServiceErrorInfo remoteServiceErrorInfo)
    {
        var exceptionHandlingOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<SnowExceptionHandlingOptions>>()
            .Value;
        var exceptionToErrorInfoConverter = context.HttpContext.RequestServices
            .GetRequiredService<IExceptionToErrorInfoConverter>();

        remoteServiceErrorInfo = exceptionToErrorInfoConverter.Convert(context.Exception, options =>
        {
            options.SendExceptionsDetailsToClients = exceptionHandlingOptions.SendExceptionsDetailsToClients;
            options.SendStackTraceToClients = exceptionHandlingOptions.SendStackTraceToClients;
            options.SendExceptionDataToClientTypes = exceptionHandlingOptions.SendExceptionDataToClientTypes;
        });

        var sb = new StringBuilder();
        sb.AppendLine($"---------- {nameof(SnowRemoteServiceErrorInfo)} ----------");
        sb.AppendLine(context.HttpContext.RequestServices
            .GetRequiredService<ISnowJsonSerializer>()
            .Serialize(remoteServiceErrorInfo, indented: true));

        if (exceptionHandlingOptions.ShouldLogException(context.Exception))
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<SnowExceptionFilter>>()
                ?? NullLogger<SnowExceptionFilter>.Instance;
            var logLevel = context.Exception.GetLogLevel();
            logger.LogWithLevel(logLevel, sb.ToString());
            logger.LogException(context.Exception, logLevel);
        }
    }
}
