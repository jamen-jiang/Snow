using System;

namespace Snow;

/// <summary>
/// 关闭控制器上的框架特性: 三个开关默认全开, 只写 <c>[DisableSnowFeatures]</c> 就是全部关掉
/// <para>对齐 ABP 的 <c>DisableAbpFeaturesAttribute</c> —— 特性放在 Snow.Core, 任何项目标注控制器都不必再引用 Snow.Security。</para>
/// <para>三个开关分别对应三处: 拦截器(AOP 代理)、框架中间件(<c>SnowMiddlewareBase</c> 的跳过判定)、MVC 过滤器。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DisableSnowFeaturesAttribute : Attribute
{
    /// <summary>
    /// 是否关闭拦截器: 该控制器对应的服务不做 AOP 代理
    /// </summary>
    public bool DisableInterceptors { get; set; } = true;

    /// <summary>
    /// 是否跳过框架中间件
    /// </summary>
    public bool DisableMiddleware { get; set; } = true;

    /// <summary>
    /// 是否关闭 MVC 过滤器: 自动校验、模型绑定校验、结果包装、异常翻译、登录态异常全部让路
    /// </summary>
    public bool DisableMvcFilters { get; set; } = true;
}
