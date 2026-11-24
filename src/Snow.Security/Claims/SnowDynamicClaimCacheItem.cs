namespace Snow.Security.Claims;

/// <summary>
/// 动态声明缓存项(对应 ABP 的 <c>AbpDynamicClaimCacheItem</c>)
/// </summary>
[Serializable]
public class SnowDynamicClaimCacheItem
{
    /// <summary>
    /// 声明列表
    /// </summary>
    public List<SnowDynamicClaim> Claims { get; set; }

    /// <summary>
    /// 构造器: 空列表
    /// </summary>
    public SnowDynamicClaimCacheItem()
    {
        Claims = new List<SnowDynamicClaim>();
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="claims">声明列表</param>
    public SnowDynamicClaimCacheItem(List<SnowDynamicClaim> claims)
    {
        Claims = claims;
    }

    /// <summary>
    /// 计算缓存键: 租户 + 用户
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="tenantId">租户Id</param>
    /// <returns>缓存键</returns>
    public static string CalculateCacheKey(Guid userId, Guid? tenantId)
    {
        return $"{tenantId}-{userId}";
    }
}
