using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.MultiTenancy;

namespace Snow.AspNetCore.MultiTenancy;

/// <summary>
/// 请求头出资者: 从请求头 <c>__tenant</c> 取租户
/// <para>键名由 <see cref="SnowAspNetCoreMultiTenancyOptions.TenantKey"/> 决定; 取不到值时什么都不做, 交给下一个出资者。</para>
/// </summary>
public sealed class HeaderTenantResolveContributor : ITenantResolveContributor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<SnowAspNetCoreMultiTenancyOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
    /// <param name="options">Web 层多租户选项</param>
    public HeaderTenantResolveContributor(IHttpContextAccessor httpContextAccessor, IOptions<SnowAspNetCoreMultiTenancyOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    /// <inheritdoc />
    public string Name => "Header";

    /// <inheritdoc />
    public Task ResolveAsync(ITenantResolveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return Task.CompletedTask;
        }

        string? value = Resolve(httpContext, _options.Value.TenantKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            context.TenantIdOrName = value;
            context.Handled = true;
        }

        return Task.CompletedTask;
    }

    private static string? Resolve(HttpContext httpContext, string tenantKey)
    {
        Microsoft.Extensions.Primitives.StringValues values = httpContext.Request.Headers[tenantKey];
        return values.Count > 0 ? values[0] : null;
    }
}
