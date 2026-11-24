using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.Options;

namespace Snow.Security.Claims;

/// <summary>
/// 动态声明贡献者基类(对应 ABP 的 <c>AbpDynamicClaimsPrincipalContributorBase</c>):
/// 子类只负责取回动态声明, 基类负责按 <see cref="SnowClaimsPrincipalFactoryOptions.ClaimsMap"/> 映射并写入主体
/// </summary>
public abstract class SnowDynamicClaimsPrincipalContributorBase : ISnowDynamicClaimsPrincipalContributor
{
    /// <inheritdoc />
    public virtual async Task ContributeAsync(SnowClaimsPrincipalContributorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var claims = await GetClaimsAsync(context);

        if (claims is null || claims.Count == 0)
        {
            return;
        }

        await AddClaimsAsync(context, claims);
    }

    /// <summary>
    /// 取回动态声明
    /// </summary>
    /// <param name="context">贡献上下文</param>
    /// <returns>动态声明列表</returns>
    protected abstract Task<List<SnowDynamicClaim>> GetClaimsAsync(SnowClaimsPrincipalContributorContext context);

    /// <summary>
    /// 把动态声明写入主体: 同类型先整体清掉再补, 值空则只清不补
    /// </summary>
    /// <param name="context">贡献上下文</param>
    /// <param name="claims">动态声明列表</param>
    /// <returns>表示写入过程的任务</returns>
    protected virtual Task AddClaimsAsync(SnowClaimsPrincipalContributorContext context, List<SnowDynamicClaim> claims)
    {
        var identity = context.ClaimsPrincipal.Identities.FirstOrDefault();

        if (identity is null)
        {
            return Task.CompletedTask;
        }

        var options = context.GetRequiredService<IOptions<SnowClaimsPrincipalFactoryOptions>>().Value;

        foreach (var claim in claims)
        {
            foreach (var mappedClaim in MapClaim(options, claim))
            {
                identity.RemoveAll(mappedClaim.Type);

                if (!string.IsNullOrWhiteSpace(mappedClaim.Value))
                {
                    identity.AddClaim(new Claim(mappedClaim.Type, mappedClaim.Value));
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 按映射表展开一条声明
    /// </summary>
    /// <param name="options">工厂选项</param>
    /// <param name="claim">动态声明</param>
    /// <returns>展开后的声明集合</returns>
    protected virtual IEnumerable<SnowDynamicClaim> MapClaim(SnowClaimsPrincipalFactoryOptions options, SnowDynamicClaim claim)
    {
        if (!options.ClaimsMap.TryGetValue(claim.Type, out var mappedTypes) || mappedTypes is not { Count: > 0 })
        {
            return new[] { claim };
        }

        return mappedTypes.Select(mappedType => new SnowDynamicClaim(mappedType, claim.Value));
    }
}
