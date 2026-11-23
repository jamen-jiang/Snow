using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Snow.AspNetCore.Filters;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 动作描述符定型后, 把标了 <see cref="DisableSnowFeaturesAttribute"/>(且开启 <see cref="DisableSnowFeaturesAttribute.DisableMvcFilters"/>)
/// 的控制器上的框架过滤器整个摘掉
/// <para>对齐 ABP 的 <c>AbpMvcActionDescriptorProvider</c>: 不是让每个过滤器在请求期自己问"要不要跳过", 而是在动作描述符生成完的
/// 那一刻一次摘除 —— 请求期零判断, 过滤器入口也不必写守卫。</para>
/// <para>摘除只认 <see cref="ServiceFilterAttribute"/>: 必须用 <c>options.Filters.AddService(typeof(X))</c> 登记框架过滤器,
/// 用 <c>options.Filters.Add&lt;X&gt;()</c> 登记得到的是 <see cref="TypeFilterAttribute"/>, 这里匹配不到, 开关对它无效。</para>
/// </summary>
public class SnowMvcActionDescriptorProvider : IActionDescriptorProvider
{
    /// <summary>
    /// 排在 MVC 自带的描述符提供器之后(-1000 + 10, 与 ABP 同值): 等框架把全局过滤器都填进描述符, 再动手摘
    /// </summary>
    public virtual int Order => -1000 + 10;

    /// <summary>
    /// 什么也不做: 摘除发生在全部提供器都跑完之后
    /// </summary>
    /// <param name="context">提供器上下文</param>
    public virtual void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
    }

    /// <summary>
    /// 逐个控制器动作检查: 关掉了框架特性就把 <see cref="ISnowFilter"/> 的过滤器描述符移除
    /// </summary>
    /// <param name="context">提供器上下文</param>
    public virtual void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (ControllerActionDescriptor action in context.Results.OfType<ControllerActionDescriptor>())
        {
            DisableSnowFeaturesAttribute? attribute = action.ControllerTypeInfo.GetCustomAttribute<DisableSnowFeaturesAttribute>(inherit: true);

            if (attribute is null || !attribute.DisableMvcFilters)
            {
                continue;
            }

            // 只摘框架自己的: 宿主用 AddService 登记的过滤器不实现 ISnowFilter, 不会被误伤
            action.FilterDescriptors.RemoveAll(filterDescriptor =>
                filterDescriptor.Filter is ServiceFilterAttribute serviceFilter
                && typeof(ISnowFilter).IsAssignableFrom(serviceFilter.ServiceType));
        }
    }
}
