using Microsoft.Extensions.DependencyInjection;

namespace Snow.AspNetCore;

/// <summary>
/// 收集中间件步骤登记
/// <para>给"必须排在某个阶段之后"的包内中间件用: 模块在 <c>OnApplicationInitialization</c> 里登记阶段与顺序,
/// 宿主在锚点用 <c>UseSnowPipeline(阶段)</c> 统一展开。挂在最外层就够的中间件不必登记,
/// 模块直接用 <c>context.GetApplicationBuilder().UseSnowMiddlewareOnce&lt;T&gt;()</c> 挂即可。</para>
/// </summary>
public sealed class SnowMiddlewareOptionsSetup
{
    private readonly List<SnowMiddlewareRegistration> _registrations = [];
    private int _sequence;

    /// <summary>
    /// 登记一条中间件步骤
    /// </summary>
    /// <param name="stage">阶段</param>
    /// <param name="order">阶段内顺序</param>
    /// <param name="implementationType">登记器或中间件类型</param>
    /// <param name="kind">挂载方式</param>
    public void Add(SnowPipelineStage stage, int order, Type implementationType, SnowMiddlewareKind kind)
    {
        ArgumentNullException.ThrowIfNull(implementationType);

        _registrations.Add(new SnowMiddlewareRegistration(stage, order, _sequence++, implementationType, kind));
    }

    /// <summary>
    /// 把已登记的步骤写入选项对象
    /// </summary>
    /// <param name="options">目标选项对象</param>
    public void Apply(SnowMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (SnowMiddlewareRegistration registration in _registrations)
        {
            options.Steps.Add(registration);
        }
    }
}
