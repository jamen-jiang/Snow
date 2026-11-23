using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Snow.AspNetCore.Filters;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 把返回值包装成 <see cref="SnowApiResponse"/> 的结果过滤器
/// <para>只处理 <see cref="ObjectResult"/>: FileResult / NoContentResult / 已经包装过的响应原样放过。</para>
/// <para>实现 <see cref="ISnowFilter"/>: 控制器标了 <see cref="DisableSnowFeaturesAttribute"/> 时由
/// <c>SnowMvcActionDescriptorProvider</c> 在动作描述符定型时把它整个摘掉, 请求期不做判断。</para>
/// </summary>
public sealed class SnowResultFilter : IAsyncResultFilter, ISnowFilter
{
    private readonly SnowMvcOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">MVC 选项</param>
    public SnowResultFilter(IOptions<SnowMvcOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Result is ObjectResult objectResult && ShouldWrap(context, objectResult))
        {
            // 换一个新结果而不是改 Value: 原结果的声明类型是动作返回类型(如 string),
            // 直接改 Value 会让 StringOutputFormatter 拿信封当字符串写, 抛 InvalidCastException
            context.Result = new ObjectResult(SnowApiResponse.Ok(objectResult.Value))
            {
                StatusCode = objectResult.StatusCode ?? StatusCodes.Status200OK,
            };
        }

        await next();
    }

    private bool ShouldWrap(ResultExecutingContext context, ObjectResult objectResult)
    {
        if (!ShouldWrap(context.ActionDescriptor))
        {
            return false;
        }

        // 已经是信封(或 ProblemDetails)的不再套一层
        return objectResult.Value is not SnowApiResponse and not ProblemDetails;
    }

    private bool ShouldWrap(ActionDescriptor actionDescriptor)
    {
        if (actionDescriptor is not ControllerActionDescriptor controllerAction)
        {
            return _options.WrapResultByDefault;
        }

        SnowWrapResultAttribute? attribute =
            controllerAction.MethodInfo.GetCustomAttributes(typeof(SnowWrapResultAttribute), inherit: true).OfType<SnowWrapResultAttribute>().FirstOrDefault()
            ?? controllerAction.ControllerTypeInfo.GetCustomAttributes(typeof(SnowWrapResultAttribute), inherit: true).OfType<SnowWrapResultAttribute>().FirstOrDefault();

        return attribute?.WrapResult ?? _options.WrapResultByDefault;
    }
}
