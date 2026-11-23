using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Snow.AspNetCore.Middleware;
using Snow.DependencyInjection;
using Snow.Tracing;

namespace Snow.AspNetCore.Tracing;

/// <summary>
/// 链路标识中间件(对应 ABP 的 <c>AbpCorrelationIdMiddleware</c>): 从请求头取链路标识, 缺失则生成一个
/// <para>整个请求生命周期内 <see cref="ICorrelationIdProvider"/> 都返回该值, 并按需写回响应头。</para>
/// </summary>
[TransientService]
public class SnowCorrelationIdMiddleware : SnowMiddlewareBase
{
    private readonly SnowCorrelationIdOptions _options;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">链路标识选项</param>
    /// <param name="correlationIdProvider">链路标识提供者</param>
    public SnowCorrelationIdMiddleware(IOptions<SnowCorrelationIdOptions> options, ICorrelationIdProvider correlationIdProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(correlationIdProvider);

        _options = options.Value;
        _correlationIdProvider = correlationIdProvider;
    }

    /// <summary>
    /// 处理请求
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="next">下一环节</param>
    /// <returns>任务</returns>
    public override async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string correlationId = GetCorrelationIdFromRequest(context);

        using (_correlationIdProvider.Change(correlationId))
        {
            SetCorrelationIdOnResponse(context, correlationId);
            await next(context);
        }
    }

    /// <summary>
    /// 从请求头取链路标识, 缺失则生成并写回请求头
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>链路标识</returns>
    protected virtual string GetCorrelationIdFromRequest(HttpContext context)
    {
        string? correlationId = context.Request.Headers[_options.HttpHeaderName];
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            context.Request.Headers[_options.HttpHeaderName] = correlationId;
        }

        return correlationId;
    }

    /// <summary>
    /// 在响应开始时把链路标识写回响应头
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="correlationId">链路标识</param>
    protected virtual void SetCorrelationIdOnResponse(HttpContext context, string correlationId)
    {
        context.Response.OnStarting(() =>
        {
            if (_options.SetResponseHeader
                && !context.Response.Headers.ContainsKey(_options.HttpHeaderName)
                && !string.IsNullOrWhiteSpace(correlationId))
            {
                context.Response.Headers[_options.HttpHeaderName] = correlationId;
            }

            return Task.CompletedTask;
        });
    }
}