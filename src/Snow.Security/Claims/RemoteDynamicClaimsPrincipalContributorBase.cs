using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.Security.Claims;

/// <summary>
/// 远程动态声明贡献者基类(对应 ABP 的 <c>RemoteDynamicClaimsPrincipalContributorBase</c>):
/// 先读缓存, 未命中再刷新; 远程调用失败时清空主体并按警告记录, 让上层重新认证而不是带着半套声明继续
/// </summary>
/// <typeparam name="TContributor">具体贡献者类型</typeparam>
/// <typeparam name="TContributorCache">该贡献者对应的缓存类型</typeparam>
public abstract class RemoteDynamicClaimsPrincipalContributorBase<TContributor, TContributorCache> : ISnowDynamicClaimsPrincipalContributor
    where TContributor : class, ISnowDynamicClaimsPrincipalContributor
    where TContributorCache : RemoteDynamicClaimsPrincipalContributorCacheBase<TContributor>
{
    /// <summary>
    /// 是否把异常细节写进日志
    /// </summary>
    protected virtual bool LogExceptionDetails => true;

    /// <inheritdoc />
    public virtual async Task ContributeAsync(SnowClaimsPrincipalContributorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var userId = context.ClaimsPrincipal.FindUserId();
        var tenantId = context.ClaimsPrincipal.FindTenantId();

        if (userId is null)
        {
            return;
        }

        List<SnowDynamicClaim>? claims;

        try
        {
            var cache = context.GetRequiredService<TContributorCache>();
            claims = await cache.GetCacheAsync(userId.Value, tenantId);
            claims ??= await cache.RefreshAsync(userId.Value, tenantId);
        }
        catch (Exception exception)
        {
            var logger = context.GetRequiredService<ILogger<RemoteDynamicClaimsPrincipalContributorBase<TContributor, TContributorCache>>>();

            if (LogExceptionDetails)
            {
                logger.LogWarning(exception, "读取远程动态声明失败, 已清空当前主体以触发重新认证");
            }
            else
            {
                logger.LogWarning("读取远程动态声明失败, 已清空当前主体以触发重新认证");
            }

            context.ClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            return;
        }

        if (claims is null || claims.Count == 0)
        {
            return;
        }

        var identity = context.ClaimsPrincipal.Identities.FirstOrDefault();

        if (identity is null)
        {
            return;
        }

        foreach (var claim in claims)
        {
            identity.RemoveAll(claim.Type);

            if (!string.IsNullOrWhiteSpace(claim.Value))
            {
                identity.AddClaim(new Claim(claim.Type, claim.Value));
            }
        }
    }
}
