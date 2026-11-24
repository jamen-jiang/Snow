using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Snow.AspNetCore.ExceptionHandling;
using Snow.AspNetCore.Filters;
using Snow.DependencyInjection;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 模型绑定/校验失败时直接返回 400 与字段错误列表的过滤器
/// <para>自动 API 的应用服务类没有 [ApiController] 特性, 所以这层行为要自己补上。</para>
/// <para>实现 <see cref="ISnowFilter"/>: 控制器标了 <see cref="DisableSnowFeaturesAttribute"/> 时由
/// <c>SnowMvcActionDescriptorProvider</c> 在动作描述符定型时把它整个摘掉, 请求期不做判断。</para>
/// </summary>
[TransientService]
public sealed class SnowModelStateInvalidFilter : IActionFilter, ISnowFilter
{
    private readonly SnowAspNetCoreMvcOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">MVC 选项</param>
    public SnowModelStateInvalidFilter(IOptions<SnowAspNetCoreMvcOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.AutoModelValidation || context.ModelState.IsValid)
        {
            return;
        }

        Dictionary<string, string[]> errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "参数不合法" : error.ErrorMessage).ToArray());

        SnowRemoteServiceErrorInfo errorInfo = new()
        {
            Code = "ValidationError",
            Message = "请求参数校验失败",
            ValidationErrors = errors,
        };

        context.Result = new ObjectResult(new SnowRemoteServiceErrorResponse(errorInfo))
        {
            StatusCode = StatusCodes.Status400BadRequest,
        };
    }

    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
