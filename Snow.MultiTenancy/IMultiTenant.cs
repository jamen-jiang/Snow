namespace Snow.MultiTenancy;

/// <summary>
/// 多租户实体契约: 实现它的实体带租户 id, 数据过滤与仓储据此按租户隔离
/// </summary>
public interface IMultiTenant
{
    /// <summary>
    /// 租户 id, null 表示宿主数据
    /// </summary>
    Guid? TenantId { get; }
}
