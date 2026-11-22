namespace Snow.AspNetCore;

/// <summary>
/// 中间件管道选项: 各模块登记的中间件步骤
/// <para>由 <see cref="SnowMiddlewareOptionsSetup"/> 收集, 宿主通过
/// <c>UseSnowPipeline(阶段)</c> 在锚点展开对应阶段的步骤。</para>
/// </summary>
public sealed class SnowMiddlewareOptions
{
    /// <summary>
    /// 已登记的步骤(按登记先后)
    /// </summary>
    public List<SnowMiddlewareRegistration> Steps { get; } = [];
}
