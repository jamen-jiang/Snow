using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Snow;
using Snow.AspNetCore.Filters;

namespace Snow.Authentication;

/// <summary>
/// MVC 异常过滤器: 登录态异常转 401/403, 其它异常原样交给后续过滤器
/// <para>实现 <see cref="ISnowFilter"/>: 控制器标了 <see cref="DisableSnowFeaturesAttribute"/> 时由
/// <c>SnowMvcActionDescriptorProvider</c>(在 <c>Snow.AspNetCore.Mvc</c>)在动作描述符定型时把它整个摘掉, 请求期不做判断。</para>
/// </summary>
public sealed class TokenExceptionFilter : IAsyncExceptionFilter, ISnowFilter
{
    /// <inheritdoc />
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await TokenExceptionHandling.TryHandleAsync(context.HttpContext, context.Exception, context.HttpContext.RequestAborted))
        {
            context.ExceptionHandled = true;
        }
    }
}
