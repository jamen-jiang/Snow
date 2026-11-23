namespace Snow.MultiTenancy;

/// <summary>
/// 租户解析结果(对齐 ABP 的 <c>TenantResolveResult</c>): id 与名称至多一个非空
/// </summary>
public sealed class TenantResolveResult
{
    /// <summary>
    /// 解析出的租户 id
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// 解析出的租户名称
    /// </summary>
    public string? TenantName { get; set; }
}
