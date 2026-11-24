using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Snow.AspNetCore.Filters;
using Snow.DependencyInjection;

namespace Snow.AspNetCore.Mvc.Response;

/// <summary>
/// 无内容动作过滤器(对应 ABP 的 <c>AbpNoContentActionFilter</c>): 动作返回 Task/void 且没有结果、响应仍是 200 时, 把状态码改成 204
/// </summary>
[TransientService]
public class SnowNoContentActionFilter : IAsyncActionFilter, ISnowFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ActionDescriptor.IsControllerAction())
        {
            await next();
            return;
        }

        await next();

        if (!context.HttpContext.Response.HasStarted &&
            context.HttpContext.Response.StatusCode == (int)HttpStatusCode.OK &&
            context.Result == null)
        {
            var returnType = context.ActionDescriptor.GetReturnType();
            if (returnType == typeof(Task) || returnType == typeof(void))
            {
                if (context.HttpContext.Response.Body.CanSeek && context.HttpContext.Response.Body.Length != 0)
                {
                    // Body 已经写过内容, 不要改状态码
                    return;
                }

                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
        }
    }
}
