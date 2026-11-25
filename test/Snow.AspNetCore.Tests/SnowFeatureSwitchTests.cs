using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Snow.AspNetCore.Filters;
using Snow.AspNetCore.Mvc;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 控制器级开关: [DisableSnowFeatures] 三个开关默认全开, 各层按控制器类型判定是否跳过
/// <para>MVC 过滤器这一路走的是 ABP 的机制: 动作描述符定型后由 <see cref="SnowMvcActionDescriptorProvider"/>
/// 把框架过滤器整个摘掉, 过滤器入口不再写请求期守卫。</para>
/// </summary>
public class SnowFeatureSwitchTests
{
    /// <summary>
    /// 只写特性不配参数时三个开关全开
    /// </summary>
    [Fact]
    public void Attribute_DefaultsToAllDisabled()
    {
        DisableSnowFeaturesAttribute attribute = new();

        Assert.True(attribute.DisableInterceptors);
        Assert.True(attribute.DisableMiddleware);
        Assert.True(attribute.DisableMvcFilters);
    }

    /// <summary>
    /// 判定按控制器类型走: 没标注、或类型为 null, 一律不跳过
    /// </summary>
    [Fact]
    public void Switches_OnlyApplyToMarkedController()
    {
        Assert.True(SnowFeatureSwitches.IsMiddlewareDisabled(typeof(MarkedController)));
        Assert.True(SnowFeatureSwitches.IsMvcFiltersDisabled(typeof(MarkedController)));
        Assert.True(SnowFeatureSwitches.IsInterceptorsDisabled(typeof(MarkedController)));
        Assert.False(SnowFeatureSwitches.IsMiddlewareDisabled(typeof(PlainController)));
        Assert.False(SnowFeatureSwitches.IsMvcFiltersDisabled(typeof(PlainController)));
        Assert.False(SnowFeatureSwitches.IsMiddlewareDisabled(null));
        Assert.False(SnowFeatureSwitches.IsInterceptorsDisabled(null));
    }

    /// <summary>
    /// 三个开关可以单独关: 只关中间件时过滤器与拦截器照常生效
    /// </summary>
    [Fact]
    public void Switches_CanBeTurnedOffIndividually()
    {
        Assert.True(SnowFeatureSwitches.IsMiddlewareDisabled(typeof(MiddlewareOnlyDisabledController)));
        Assert.False(SnowFeatureSwitches.IsMvcFiltersDisabled(typeof(MiddlewareOnlyDisabledController)));
        Assert.False(SnowFeatureSwitches.IsInterceptorsDisabled(typeof(MiddlewareOnlyDisabledController)));
    }

    /// <summary>
    /// 标了特性的控制器: 描述符定型后框架过滤器被整个摘掉
    /// </summary>
    [Fact]
    public void Provider_RemovesFrameworkFiltersOnMarkedController()
    {
        ControllerActionDescriptor action = CreateControllerAction(typeof(MarkedController), typeof(FrameworkFilter));

        RunProvider(action);

        Assert.Empty(action.FilterDescriptors);
    }

    /// <summary>
    /// 开关单独关掉时不摘: 只关了拦截器与中间件的控制器, 过滤器照旧
    /// </summary>
    [Fact]
    public void Provider_KeepsFrameworkFiltersWhenSwitchOff()
    {
        ControllerActionDescriptor action = CreateControllerAction(typeof(MiddlewareOnlyDisabledController), typeof(FrameworkFilter));

        RunProvider(action);

        Assert.Single(action.FilterDescriptors);
    }

    /// <summary>
    /// 只摘框架自己的: 宿主用 AddService 登记的过滤器不实现 ISnowFilter, 不会被误伤
    /// </summary>
    [Fact]
    public void Provider_KeepsFiltersThatAreNotFrameworkFilters()
    {
        ControllerActionDescriptor action = CreateControllerAction(typeof(MarkedController), typeof(HostFilter), typeof(FrameworkFilter));

        RunProvider(action);

        FilterDescriptor remaining = Assert.Single(action.FilterDescriptors);
        Assert.IsType<ServiceFilterAttribute>(remaining.Filter);
    }

    /// <summary>
    /// 不是控制器动作的端点(最小 API 等)不参与判定
    /// </summary>
    [Fact]
    public void Provider_IgnoresNonControllerActions()
    {
        ActionDescriptor action = new() { FilterDescriptors = CreateFilterDescriptors(typeof(FrameworkFilter)) };

        RunProvider(action);

        Assert.Single(action.FilterDescriptors);
    }

    private static void RunProvider(ActionDescriptor action)
    {
        ActionDescriptorProviderContext context = new();
        context.Results.Add(action);

        new SnowMvcActionDescriptorProvider().OnProvidersExecuted(context);
    }

    private static ControllerActionDescriptor CreateControllerAction(Type controllerType, params Type[] filterTypes)
    {
        return new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            MethodInfo = controllerType.GetMethod(nameof(PlainController.Get))!,
            FilterDescriptors = CreateFilterDescriptors(filterTypes),
        };
    }

    // 描述符自带的 FilterDescriptors 是只读空数组, 要自己换成可写的 List
    private static List<FilterDescriptor> CreateFilterDescriptors(params Type[] filterTypes)
    {
        return [.. filterTypes.Select(filterType => new FilterDescriptor(new ServiceFilterAttribute(filterType), 0))];
    }

    [DisableSnowFeatures]
    private sealed class MarkedController
    {
        public string Get() => "ok";
    }

    [DisableSnowFeatures(DisableInterceptors = false, DisableMvcFilters = false)]
    private sealed class MiddlewareOnlyDisabledController
    {
        public string Get() => "ok";
    }

    private sealed class PlainController
    {
        public string Get() => "ok";
    }

    /// <summary>
    /// 宿主自己的过滤器: 不实现 ISnowFilter, 摘除时应当留着
    /// </summary>
    private sealed class HostFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

    /// <summary>
    /// 框架过滤器: 实现 ISnowFilter, 用于验证特性开关摘除
    /// </summary>
    private sealed class FrameworkFilter : IActionFilter, ISnowFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
