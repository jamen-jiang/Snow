using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Snow.AspNetCore;

/// <summary>
/// 管道组装器: 框架固定骨架 + 包登记的过滤器 + 宿主追加的中间件
/// <para>骨架写死在这里, 顺序是: 前置钩子 → 链路标识 → 前置路由钩子 → <c>UseRouting()</c> → 后置路由钩子 →
/// <c>UseAuthentication()</c> → <c>UseAuthorization()</c> → 后置钩子 → <c>UseEndpoints()</c>(前置/后置端点钩子之间)。</para>
/// <para>包只能选位置, 不能改顺序: 位置是七个具名钩子之一, 同一钩子里按登记先后执行(即模块依赖的拓扑序)。</para>
/// </summary>
internal sealed class SnowApplicationBuilder : ISnowApplicationBuilder, ISnowEndpointBuilder, ISnowApplicationBuilderContext, ISnowEndpointBuilderContext
{
    private readonly SnowPipelineOptions _options;
    private readonly ILogger? _logger;

    /// <summary>
    /// 建一个管道组装器
    /// </summary>
    /// <param name="appBuilder">应用构建器</param>
    public SnowApplicationBuilder(IApplicationBuilder appBuilder)
    {
        ArgumentNullException.ThrowIfNull(appBuilder);

        AppBuilder = appBuilder;
        ApplicationServices = appBuilder.ApplicationServices;
        _options = ApplicationServices.GetRequiredService<IOptions<SnowPipelineOptions>>().Value;
        _logger = ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger<SnowApplicationBuilder>();
    }

    /// <inheritdoc />
    public IApplicationBuilder AppBuilder { get; }

    /// <inheritdoc />
    public IServiceProvider ApplicationServices { get; }

    /// <inheritdoc />
    public IEndpointRouteBuilder EndpointRouteBuilder { get; private set; } = null!;

    /// <inheritdoc />
    public ISnowEndpointBuilder WithCustomMiddleware(Action<ISnowApplicationBuilderContext> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        // 自定义管道: 骨架与过滤器都不跑, 完全由宿主决定
        configure(this);
        LogPipeline("自定义");
        return this;
    }

    /// <inheritdoc />
    public ISnowEndpointBuilder WithMiddleware(Action<ISnowApplicationBuilderContext> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        RunPrePipeline();
        RegisterDefaultRequiredMiddleware();
        RunPostPipeline();
        configure(this);
        LogPipeline("默认");
        return this;
    }

    /// <inheritdoc />
    public void WithEndpoints(Action<ISnowEndpointBuilderContext> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        // 端点钩子在 UseEndpoints 之前跑, 与 Umbraco 的 RunPreEndpointsPipeline 同形
        foreach (ISnowPipelineFilter filter in _options.PipelineFilters)
        {
            filter.OnEndpoints(AppBuilder);
        }

        // WebApplication 自己就是端点路由构建器, 直接映射; 通用宿主下走 UseEndpoints
        if (AppBuilder is IEndpointRouteBuilder endpoints)
        {
            MapEndpoints(endpoints, configure);
            return;
        }

        AppBuilder.UseEndpoints(endpoints => MapEndpoints(endpoints, configure));
    }

    /// <inheritdoc />
    public void UseSnowCoreMiddleware()
    {
        // 链路标识必须在最外层: 认证失败、授权拒绝、路由没匹配时响应也要带得上
        AppBuilder.UseSnowCorrelationId();
    }

    /// <inheritdoc />
    public void RegisterDefaultRequiredMiddleware()
    {
        UseSnowCoreMiddleware();

        RunPreRouting();

        // 没有登记路由服务的宿主(纯中间件应用)不挂路由, 否则启动期就抛
        if (ApplicationServices.GetService<EndpointDataSource>() is not null)
        {
            AppBuilder.UseRouting();
        }

        RunPostRouting();

        // 认证与授权按容器里有没有对应服务决定: 宿主可能压根没引认证包
        if (ApplicationServices.GetService<IAuthenticationSchemeProvider>() is not null)
        {
            AppBuilder.UseAuthentication();
        }

        if (ApplicationServices.GetService<IAuthorizationPolicyProvider>() is not null)
        {
            AppBuilder.UseAuthorization();
        }
    }

    /// <inheritdoc />
    public void RunPrePipeline() => RunFilters(filter => filter.OnPrePipeline(AppBuilder));

    /// <inheritdoc />
    public void RunPreRouting() => RunFilters(filter => filter.OnPreRouting(AppBuilder));

    /// <inheritdoc />
    public void RunPostRouting() => RunFilters(filter => filter.OnPostRouting(AppBuilder));

    /// <inheritdoc />
    public void RunPostPipeline() => RunFilters(filter => filter.OnPostPipeline(AppBuilder));

    /// <summary>
    /// 跑一遍所有过滤器
    /// </summary>
    /// <param name="action">对单个过滤器要做的动作</param>
    private void RunFilters(Action<ISnowPipelineFilter> action)
    {
        foreach (ISnowPipelineFilter filter in _options.PipelineFilters)
        {
            action(filter);
        }
    }

    /// <summary>
    /// 在端点路由构建器上跑端点钩子与映射动作
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <param name="configure">宿主/框架的映射动作</param>
    private void MapEndpoints(IEndpointRouteBuilder endpoints, Action<ISnowEndpointBuilderContext> configure)
    {
        EndpointRouteBuilder = endpoints;

        foreach (ISnowPipelineFilter filter in _options.PipelineFilters)
        {
            filter.OnPreMapEndpoints(endpoints);
        }

        configure(this);

        foreach (ISnowPipelineFilter filter in _options.PipelineFilters)
        {
            filter.OnPostMapEndpoints(endpoints);
        }
    }

    /// <summary>
    /// 把这次组装的结果打进日志: 挂了几个过滤器、分别在哪个位置
    /// </summary>
    /// <param name="kind">骨架类型</param>
    private void LogPipeline(string kind)
    {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        List<string> names = [];
        foreach (ISnowPipelineFilter filter in _options.PipelineFilters)
        {
            names.Add(filter.Name);
        }

        _logger.LogInformation(
            "HTTP 管道({Kind}): 前置 → 链路标识 → 路由 → 认证 → 授权 → 后置 → 端点; 过滤器 {Count} 个: {Filters}",
            kind,
            names.Count,
            names.Count == 0 ? "(无)" : string.Join(", ", names));
    }
}
