using Microsoft.Extensions.DependencyInjection;
using Snow.Security.Users;

namespace Snow.MultiTenancy;

/// <summary>
/// 当前用户出资者: 已登录用户带的 <c>tenantid</c> 声明即所属租户
/// <para>由 <see cref="SnowMultiTenancyModule"/> 显式登记在最前面, 与 ABP 一致:
/// 用户身份比 URL 参数更可信, 避免前端随手改 query 就越权到别的租户。</para>
/// </summary>
public sealed class CurrentUserTenantResolveContributor : ITenantResolveContributor
{
    /// <inheritdoc />
    public string Name => "CurrentUser";

    /// <inheritdoc />
    public Task ResolveAsync(ITenantResolveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ICurrentUser? currentUser = context.ServiceProvider.GetService<ICurrentUser>();
        Guid? tenantId = currentUser?.TenantId;
        if (tenantId.HasValue)
        {
            context.TenantIdOrName = tenantId.Value.ToString();
            context.Handled = true;
        }

        return Task.CompletedTask;
    }
}
