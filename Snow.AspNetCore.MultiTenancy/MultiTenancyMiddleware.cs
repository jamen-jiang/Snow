using Microsoft.AspNetCore.Http;
using Snow.MultiTenancy;

namespace Snow.AspNetCore.MultiTenancy;

/// <summary>
/// 多租户中间件(对齐 ABP 的 <c>MultiTenancyMiddleware</c>)
/// <para>每个请求跑一遍租户解析, 解析出就把请求包进 <see cref="ICurrentTenant.Change(Guid?, string?)"/> 里, 请求结束自动还原。
/// 解析出了值但存储里没有对应租户时返回 404, 而不是悄悄当宿主处理。</para>
/// <para>未启用多租户时原样放行, 零开销。</para>
/// </summary>
public sealed class MultiTenancyMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="next">下一个中间件</param>
    public MultiTenancyMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// 中间件执行体
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="multiTenancyConfig">多租户开关</param>
    /// <param name="tenantConfigurationProvider">租户配置提供器</param>
    /// <param name="currentTenant">当前租户</param>
    /// <returns>异步任务</returns>
    public async Task InvokeAsync(
        HttpContext context,
        IMultiTenancyConfig multiTenancyConfig,
        ITenantConfigurationProvider tenantConfigurationProvider,
        ICurrentTenant currentTenant)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!multiTenancyConfig.IsEnabled)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        TenantConfiguration? tenant;
        try
        {
            tenant = await tenantConfigurationProvider.GetAsync(saveResolveResult: true, context.RequestAborted).ConfigureAwait(false);
        }
        catch (SnowTenantNotFoundException exception)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(
                new { error = new { code = "TenantNotFound", message = exception.Message } },
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        using (currentTenant.Change(tenant?.Id, tenant?.Name))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}
