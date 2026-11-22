using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 中间件管道的应用扩展: 在宿主管道的锚点处展开各包登记的步骤
/// </summary>
public static class SnowMiddlewareApplicationBuilderExtensions
{
    /// <summary>
    /// 展开某个阶段登记的中间件(按顺序常量排序)
    /// <para>宿主管道里每个锚点调用一次; 同一阶段调两次会抛异常。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="stage">阶段</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowPipeline(this IApplicationBuilder app, SnowPipelineStage stage)
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowMiddlewareOptions options = app.ApplicationServices
            .GetRequiredService<IOptions<SnowMiddlewareOptions>>()
            .Value;
        SnowPipelineState state = app.ApplicationServices.GetRequiredService<SnowPipelineState>();

        Validate(options);
        state.MarkStageApplied(stage);

        IEnumerable<SnowMiddlewareRegistration> steps = options.Steps
            .Where(step => step.Stage == stage)
            .OrderBy(step => step.Order)
            .ThenBy(step => step.Sequence);

        foreach (SnowMiddlewareRegistration step in steps)
        {
            // 手动 Use 过同一中间件时不再重复挂载
            if (!state.TryMarkApplied(step.Key))
            {
                continue;
            }

            if (step.Kind == SnowMiddlewareKind.Step)
            {
                ISnowMiddlewareStep instance = (ISnowMiddlewareStep)ActivatorUtilities
                    .GetServiceOrCreateInstance(app.ApplicationServices, step.ImplementationType);
                instance.Configure(app);
            }
            else
            {
                app.UseMiddleware(step.ImplementationType);
            }
        }

        return app;
    }

    /// <summary>
    /// 展开某个阶段, 并紧接着挂上宿主自己的中间件
    /// <para>宿主的中间件是原生写法, 不经任何登记: <c>p.Use(...)</c> / <c>p.UseMiddleware&lt;T&gt;()</c> / <c>p.Map</c> / <c>p.UseWhen</c> 全部可用。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="stage">阶段</param>
    /// <param name="configure">宿主自己的管道配置</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowStage(
        this IApplicationBuilder app, SnowPipelineStage stage, Action<IApplicationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configure);

        app.UseSnowPipeline(stage);
        configure(app);
        return app;
    }

    /// <summary>
    /// 挂载一个中间件, 与管道登记的同一中间件互斥(只生效一次)
    /// <para>包内 <c>UseSnowXxx()</c> 扩展方法用它, 保证宿主"手动挂"与"锚点展开"同时存在时不重复。</para>
    /// </summary>
    /// <typeparam name="TMiddleware">中间件类型</typeparam>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器本身</returns>
    public static IApplicationBuilder UseSnowMiddlewareOnce<TMiddleware>(this IApplicationBuilder app)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(app);

        SnowPipelineState state = app.ApplicationServices.GetRequiredService<SnowPipelineState>();
        if (!state.TryMarkApplied(typeof(TMiddleware).FullName ?? typeof(TMiddleware).Name))
        {
            return app;
        }

        return app.UseMiddleware<TMiddleware>();
    }

    private static void Validate(SnowMiddlewareOptions options)
    {
        foreach (IGrouping<SnowPipelineStage, SnowMiddlewareRegistration> group in options.Steps.GroupBy(step => step.Stage))
        {
            IGrouping<int, SnowMiddlewareRegistration>? duplicate = group
                .GroupBy(step => step.Order)
                .FirstOrDefault(orders => orders.Count() > 1);

            if (duplicate is not null)
            {
                throw new SnowInitializationException(
                    "中间件管道阶段 " + group.Key + " 出现重复顺序 " + duplicate.Key + ": "
                    + string.Join(", ", duplicate.Select(step => step.ImplementationType.Name))
                    + "。请改用不同的顺序值(宿主从 SnowMiddlewareOrders.HostOffset 开始递增)。");
            }
        }
    }
}
