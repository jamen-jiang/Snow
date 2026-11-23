using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.MultiTenancy;
using Snow.AspNetCore.Middleware;
using Snow.Security.Users;
using Snow.Tracing;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace Snow.AspNetCore.Serilog;

/// <summary>
/// 请求级日志增强中间件: 把当前用户/租户/链路标识压进 Serilog 的 LogContext, 请求内所有日志自动带上
/// <para>对齐 ABP 的 <c>AbpSerilogMiddleware</c>, 但增强值改为写日志那一刻才读取:
/// 这样中间件挂在管道哪个位置都不影响结果(即使挂在认证/多租户之前, 内层解析出的租户与用户也能带上)。</para>
/// </summary>
[TransientService]
public class SnowSerilogMiddleware : SnowMiddlewareBase
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly SnowAspNetCoreSerilogOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="currentTenant">当前租户</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="correlationIdProvider">链路标识提供者</param>
    /// <param name="options">增强选项</param>
    public SnowSerilogMiddleware(
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        ICorrelationIdProvider correlationIdProvider,
        IOptions<SnowAspNetCoreSerilogOptions> options)
    {
        ArgumentNullException.ThrowIfNull(currentTenant);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(correlationIdProvider);
        ArgumentNullException.ThrowIfNull(options);

        _currentTenant = currentTenant;
        _currentUser = currentUser;
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
        using (LogContext.Push(new SnowLogContextEnricher(_currentTenant, _currentUser, _correlationIdProvider, _options.EnricherPropertyNames)))
        {
            await next(context);
        }
    }

    /// <summary>
    /// 惰性日志增强器: 每条日志创建时读取当前租户/用户/链路标识
    /// <para>值在 <see cref="Enrich"/> 调用时读取, 因此与中间件在管道中的位置无关。</para>
    /// </summary>
    private sealed class SnowLogContextEnricher : ILogEventEnricher
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ICurrentUser _currentUser;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly SnowAspNetCoreSerilogOptions.AllEnricherPropertyNames _names;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="currentTenant">当前租户</param>
        /// <param name="currentUser">当前用户</param>
        /// <param name="correlationIdProvider">链路标识提供者</param>
        /// <param name="names">写进日志事件的属性名</param>
        public SnowLogContextEnricher(
            ICurrentTenant currentTenant,
            ICurrentUser currentUser,
            ICorrelationIdProvider correlationIdProvider,
            SnowAspNetCoreSerilogOptions.AllEnricherPropertyNames names)
        {
            _currentTenant = currentTenant;
            _currentUser = currentUser;
            _correlationIdProvider = correlationIdProvider;
            _names = names;
        }

        /// <inheritdoc />
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            ArgumentNullException.ThrowIfNull(logEvent);
            ArgumentNullException.ThrowIfNull(propertyFactory);

            if (_currentTenant?.Id is { } tenantId)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(_names.TenantId, tenantId));
            }

            if (_currentUser?.Id is { } userId)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(_names.UserId, userId));
            }

            string? correlationId = _correlationIdProvider.Get();
            if (!string.IsNullOrEmpty(correlationId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(_names.CorrelationId, correlationId));
            }
        }
    }
}
