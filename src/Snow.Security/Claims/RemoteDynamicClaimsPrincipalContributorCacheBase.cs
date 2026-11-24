namespace Snow.Security.Claims;

/// <summary>
/// 远程动态声明缓存基类(对应 ABP 的 <c>RemoteDynamicClaimsPrincipalContributorCacheBase</c>):
/// 具体缓存实现(分布式缓存 / HTTP 调用等)由子类决定
/// </summary>
/// <typeparam name="TContributor">使用该缓存的动态贡献者类型</typeparam>
public abstract class RemoteDynamicClaimsPrincipalContributorCacheBase<TContributor>
    where TContributor : class, ISnowDynamicClaimsPrincipalContributor
{
    /// <summary>
    /// 读缓存
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="tenantId">租户Id</param>
    /// <returns>动态声明列表, 未命中返回 null</returns>
    public abstract Task<List<SnowDynamicClaim>?> GetCacheAsync(long userId, Guid? tenantId);

    /// <summary>
    /// 刷新缓存
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="tenantId">租户Id</param>
    /// <returns>动态声明列表</returns>
    public abstract Task<List<SnowDynamicClaim>> RefreshAsync(long userId, Guid? tenantId);
}
