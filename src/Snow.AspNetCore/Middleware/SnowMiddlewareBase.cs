using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Snow.AspNetCore.Middleware;

/// <summary>
/// 框架中间件基类: 实现 <see cref="IMiddleware"/> 后由容器构造, 构造器可以自由注入服务
/// <para>对齐 ABP 的 <c>AbpMiddlewareBase</c>; 中间件因此可以声明任意依赖, 不受 UseMiddleware 只注入 RequestDelegate 的限制。</para>
/// <para>放在接线包而非 <c>Snow.Security</c>: 它依赖 ASP.NET Core 的 <see cref="IMiddleware"/> 与 MVC 的 <c>ControllerActionDescriptor</c>,
/// 而 <c>Snow.Security</c> 是安全契约包(当前用户 / ClaimsPrincipal 抽象), 不该替宿主决定用哪个 Web 栈。</para>
/// </summary>
public abstract class SnowMiddlewareBase : IMiddleware
{
    /// <summary>
    /// 判断当前请求是否应跳过本中间件(控制器标注了 <see cref="DisableSnowFeaturesAttribute"/> 且开启跳过)
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="next">下一环节</param>
    /// <returns>是否跳过</returns>
    protected virtual Task<bool> ShouldSkipAsync(HttpContext context, RequestDelegate next)
    {
        // 只认控制器端点: 最小 API / 静态文件没有 ControllerActionDescriptor, 一律不跳过
        ControllerActionDescriptor? controllerActionDescriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();

        return Task.FromResult(SnowFeatureSwitches.IsMiddlewareDisabled(controllerActionDescriptor?.ControllerTypeInfo));
    }

    /// <summary>
    /// 处理请求
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="next">下一环节</param>
    /// <returns>任务</returns>
    public abstract Task InvokeAsync(HttpContext context, RequestDelegate next);
}