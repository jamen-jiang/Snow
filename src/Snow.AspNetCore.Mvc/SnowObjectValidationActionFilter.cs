using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Snow.AspNetCore.Filters;
using Snow.Validation;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// MVC 自动校验过滤器: 进 Action 之前用 <see cref="ISnowObjectValidator"/> 校验每个复杂参数, 有错误直接回 400 与字段错误列表
/// <para>对应 ABP 的 <c>AbpValidationActionFilter</c>: 校验能力本身来自 Snow.Validation(DataAnnotations + FluentValidation 合成),
/// MVC 层只负责在调用 Action 之前跑一遍, 所以不需要单独的校验 AspNetCore 包。</para>
/// <para>排在管线最前(Order = -1000), 使校验失败先于业务代码与结果包装。</para>
/// <para>实现 <see cref="ISnowFilter"/>: 控制器标了 <see cref="DisableSnowFeaturesAttribute"/> 时由
/// <c>SnowMvcActionDescriptorProvider</c> 在动作描述符定型时把它整个摘掉, 请求期不做判断。</para>
/// </summary>
public sealed class SnowObjectValidationActionFilter : IAsyncActionFilter, ISnowFilter
{
    private readonly ISnowObjectValidator _validator;
    private readonly SnowMvcOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="validator">对象校验器(DataAnnotations 与 FluentValidation 合成的管线)</param>
    /// <param name="options">MVC 选项, 用 <see cref="SnowMvcOptions.AutoModelStateValidation"/> 控制开关</param>
    public SnowObjectValidationActionFilter(ISnowObjectValidator validator, IOptions<SnowMvcOptions> options)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!_options.AutoModelStateValidation)
        {
            await next().ConfigureAwait(false);
            return;
        }

        List<ValidationResult> errors = [];

        foreach (ParameterDescriptor parameter in context.ActionDescriptor.Parameters)
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out object? value))
            {
                continue;
            }

            if (value is null || value is CancellationToken || SnowValidationHelper.IsSimpleType(parameter.ParameterType))
            {
                continue;
            }

            errors.AddRange(_validator.Validate(value));
        }

        if (errors.Count == 0)
        {
            await next().ConfigureAwait(false);
            return;
        }

        Dictionary<string, string[]> validationErrors = errors
            .GroupBy(error => error.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage ?? "参数不合法").ToArray());

        context.Result = new ObjectResult(SnowApiResponse.Fail("ValidationError", "请求参数校验失败", validationErrors: validationErrors))
        {
            StatusCode = StatusCodes.Status400BadRequest,
        };
    }
}
