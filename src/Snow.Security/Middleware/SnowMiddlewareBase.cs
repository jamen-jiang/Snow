using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Snow.Security.Middleware;

/// <summary>
/// 框架中间件基类: 实现 <see cref="IMiddleware"/> 后由容器构造, 构造器可以自由注入服务
/// <para>对齐 ABP 的 <c>AbpMiddlewareBase</c>; 中间件因此可以声明任意依赖, 不受 UseMiddleware 只注入 RequestDelegate 的限制。</para>
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
        Endpoint? endpoint = context.GetEndpoint();
        ControllerActionDescriptor? controllerActionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        DisableSnowFeaturesAttribute? disableSnowFeaturesAttribute = controllerActionDescriptor?.ControllerTypeInfo.GetCustomAttribute<DisableSnowFeaturesAttribute>();

        return Task.FromResult(disableSnowFeaturesAttribute != null && disableSnowFeaturesAttribute.DisableMiddleware);
    }

    /// <summary>
    /// 处理请求
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="next">下一环节</param>
    /// <returns>任务</returns>
    public abstract Task InvokeAsync(HttpContext context, RequestDelegate next);
}
