using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Snow.Aop;

namespace Snow.Auditing;

/// <summary>
/// 审计注解: 标在类或方法上, 被拦截的调用自动记一条审计日志(调用方、耗时、参数、异常)
/// <example>
/// <code>
/// [Auditing]
/// public sealed class OrderAppService : IOrderAppService { ... }
/// </code>
/// </example>
/// <para>捕获异常后照常抛出, 不影响业务语义; 保存失败由 <see cref="IAuditingManager"/> 吞掉并记错误日志。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuditingAttribute : SnowInterceptorAttribute
{
    /// <inheritdoc />
    public override async Task InvokeAsync(AspectContext context, AspectDelegate next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        IAuditingHelper helper = context.ServiceProvider.GetRequiredService<IAuditingHelper>();
        if (!helper.ShouldSaveAudit(context.Method))
        {
            await next(context, cancellationToken).ConfigureAwait(false);
            return;
        }

        IAuditingManager auditingManager = context.ServiceProvider.GetRequiredService<IAuditingManager>();
        AuditLogInfo logInfo = helper.CreateAuditLogInfo();
        AuditLogActionInfo actionInfo = helper.CreateAuditLogActionInfo(context);
        logInfo.Actions.Add(actionInfo);

        using IAuditLogScope scope = auditingManager.BeginScope(logInfo);

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logInfo.Exceptions.Add(AuditLogException.From(exception));

            throw;
        }
        finally
        {
            stopwatch.Stop();
            logInfo.ExecutionDuration = stopwatch.ElapsedMilliseconds;
            actionInfo.ExecutionDuration = stopwatch.ElapsedMilliseconds;

            // 状态码在方法执行完后才确定, 这里再刷一次 HTTP 字段
            helper.FillHttpContext(logInfo);

            await auditingManager.SaveAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
