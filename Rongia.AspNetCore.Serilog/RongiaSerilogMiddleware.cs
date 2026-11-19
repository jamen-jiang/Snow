using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Rongia.Clients;
using Rongia.DependencyInjection;
using Rongia.MultiTenancy;
using Rongia.Security.Middleware;
using Rongia.Security.Users;
using Rongia.Tracing;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Rongia.AspNetCore.Serilog;

/// <summary>
/// 请求级日志增强中间件: 把当前租户/用户/客户端/链路标识压进 Serilog 的 LogContext, 请求内所有日志自动带上
/// <para>对齐 ABP 的 <c>AbpSerilogMiddleware</c>。</para>
/// </summary>
public class RongiaSerilogMiddleware : RongiaMiddlewareBase, ITransientDependency
{
    private readonly ICurrentClient _currentClient;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly RongiaAspNetCoreSerilogOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="currentTenant">当前租户</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="currentClient">当前客户端</param>
    /// <param name="correlationIdProvider">链路标识提供者</param>
    /// <param name="options">增强选项</param>
    public RongiaSerilogMiddleware(
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        ICurrentClient currentClient,
        ICorrelationIdProvider correlationIdProvider,
        IOptions<RongiaAspNetCoreSerilogOptions> options)
    {
        ArgumentNullException.ThrowIfNull(currentTenant);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(currentClient);
        ArgumentNullException.ThrowIfNull(correlationIdProvider);
        ArgumentNullException.ThrowIfNull(options);

        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _currentClient = currentClient;
        _correlationIdProvider = correlationIdProvider;
        _options = options.Value;
    }

    /// <summary>
    /// 处理请求
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="next">下一环节</param>
    /// <returns>任务</returns>
    public override async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        List<ILogEventEnricher> enrichers = [];

        if (_currentTenant?.Id != null)
        {
            enrichers.Add(new PropertyEnricher(_options.EnricherPropertyNames.TenantId, _currentTenant.Id));
        }

        if (_currentUser?.Id != null)
        {
            enrichers.Add(new PropertyEnricher(_options.EnricherPropertyNames.UserId, _currentUser.Id));
        }

        if (_currentClient?.Id != null)
        {
            enrichers.Add(new PropertyEnricher(_options.EnricherPropertyNames.ClientId, _currentClient.Id));
        }

        string? correlationId = _correlationIdProvider.Get();
        if (!string.IsNullOrEmpty(correlationId))
        {
            enrichers.Add(new PropertyEnricher(_options.EnricherPropertyNames.CorrelationId, correlationId));
        }

        using (LogContext.Push([.. enrichers]))
        {
            await next(context);
        }
    }
}
