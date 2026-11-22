namespace Snow.MultiTenancy;

/// <summary>
/// 租户不存在异常
/// </summary>
public sealed class SnowTenantNotFoundException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="tenantId">租户 id</param>
    /// <param name="tenantName">租户名称</param>
    public SnowTenantNotFoundException(Guid? tenantId, string? tenantName)
        : base(BuildMessage(tenantId, tenantName))
    {
        TenantId = tenantId;
        TenantName = tenantName;
    }

    /// <summary>
    /// 租户 id
    /// </summary>
    public Guid? TenantId { get; }

    /// <summary>
    /// 租户名称
    /// </summary>
    public string? TenantName { get; }

    private static string BuildMessage(Guid? tenantId, string? tenantName)
    {
        if (tenantId.HasValue)
        {
            return "找不到租户: " + tenantId.Value;
        }

        return tenantName is null ? "找不到租户。" : "找不到租户: " + tenantName;
    }
}
