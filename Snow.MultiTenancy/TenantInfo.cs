namespace Snow.MultiTenancy;

/// <summary>
/// 当前租户的最小信息, 存在 <see cref="ICurrentTenantAccessor"/> 里
/// </summary>
public sealed class TenantInfo
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="tenantId">租户 id, null 表示宿主</param>
    /// <param name="name">租户名称</param>
    public TenantInfo(Guid? tenantId = null, string? name = null)
    {
        TenantId = tenantId;
        Name = name;
    }

    /// <summary>
    /// 租户 id, null 表示宿主
    /// </summary>
    public Guid? TenantId { get; }

    /// <summary>
    /// 租户名称
    /// </summary>
    public string? Name { get; }
}
