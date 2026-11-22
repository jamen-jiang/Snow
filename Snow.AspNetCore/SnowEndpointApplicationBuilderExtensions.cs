using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 端点统一挂载
/// </summary>
public static class SnowEndpointApplicationBuilderExtensions
{
    /// <summary>
    /// 执行各模块登记的端点配置动作, 一次性把 Controller / Hub / 健康检查等端点挂完
    /// <para>对应 ABP 的 <c>UseConfiguredEndpoints()</c>。宿主只需在模块初始化之后调用一次,
    /// 不用再逐个调用 <c>UseSnowControllers()</c> / <c>MapSnowHubs()</c> / <c>MapSnowHealthChecks()</c>。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="additionalConfigurationAction">额外的端点配置动作</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowEndpoints(
        this IApplicationBuilder app,
        Action<IEndpointRouteBuilder>? additionalConfigurationAction = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        VerifyPipelineStages(app);

        SnowEndpointRouterOptions options = app.ApplicationServices
            .GetRequiredService<IOptions<SnowEndpointRouterOptions>>()
            .Value;

        if (options.EndpointConfigureActions.Count == 0 && additionalConfigurationAction is null)
        {
            return app;
        }

        // WebApplication 自身就是 IEndpointRouteBuilder, 直接往上映射即可, 不需要 UseRouting + UseEndpoints 那一套
        if (app is IEndpointRouteBuilder endpointRouteBuilder)
        {
            Apply(app, endpointRouteBuilder, options, additionalConfigurationAction);
            return app;
        }

        return app.UseEndpoints(endpoints => Apply(app, endpoints, options, additionalConfigurationAction));
    }

    /// <summary>
    /// 检查"已登记但从未展开"的中间件阶段: 只在宿主已经用了管道锚点时严格检查(老宿主继续手动 Use 不受影响)
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <exception cref="SnowInitializationException">有阶段登记了中间件却没有对应的锚点调用</exception>
    private static void VerifyPipelineStages(IApplicationBuilder app)
    {
        SnowPipelineState state = app.ApplicationServices.GetRequiredService<SnowPipelineState>();
        if (state.AppliedStages.Count == 0)
        {
            return;
        }

        SnowMiddlewareOptions options = app.ApplicationServices
            .GetRequiredService<IOptions<SnowMiddlewareOptions>>()
            .Value;

        List<SnowPipelineStage> missing = options.Steps
            .Select(step => step.Stage)
            .Distinct()
            .Where(stage => !state.IsStageApplied(stage))
            .ToList();

        if (missing.Count > 0)
        {
            throw new SnowInitializationException(
                "以下中间件阶段已登记但没有展开, 请在管道里补上 UseSnowPipeline(SnowPipelineStage.Xxx): "
                + string.Join(", ", missing));
        }
    }

    private static void Apply(
        IApplicationBuilder app,
        IEndpointRouteBuilder endpoints,
        SnowEndpointRouterOptions options,
        Action<IEndpointRouteBuilder>? additionalConfigurationAction)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        SnowEndpointRouteBuilderContext context = new(endpoints, scope.ServiceProvider);

        foreach (Action<SnowEndpointRouteBuilderContext> configure in options.EndpointConfigureActions)
        {
            configure(context);
        }

        additionalConfigurationAction?.Invoke(endpoints);
    }
}