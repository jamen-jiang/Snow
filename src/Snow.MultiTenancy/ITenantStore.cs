namespace Snow.MultiTenancy;

/// <summary>
/// 租户存储契约(对齐 ABP 的 <c>ITenantStore</c>)
/// <para>框架内用 <see cref="NullTenantStore"/> 兜底; 实际应用把它换成读数据库的实现。</para>
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// 按 id 或名称查找租户, 不存在返回 null
    /// </summary>
    /// <param name="id">租户 id, 优先使用</param>
    /// <param name="name">租户名称, id 为空时使用</param>
    /// <returns>租户配置, 不存在为 null</returns>
    Task<TenantConfiguration?> FindAsync(Guid? id, string? name = null);

    /// <summary>
    /// 按 id 查找租户, 不存在返回 null
    /// </summary>
    /// <param name="id">租户 id</param>
    /// <returns>租户配置, 不存在为 null</returns>
    Task<TenantConfiguration?> FindAsync(Guid id);

    /// <summary>
    /// 按名称查找租户, 不存在返回 null
    /// </summary>
    /// <param name="name">租户名称</param>
    /// <returns>租户配置, 不存在为 null</returns>
    Task<TenantConfiguration?> FindAsync(string name);

    /// <summary>
    /// 按 id 取租户, 不存在抛 <see cref="SnowTenantNotFoundException"/>
    /// </summary>
    /// <param name="id">租户 id</param>
    /// <returns>租户配置</returns>
    Task<TenantConfiguration> GetAsync(Guid id);

    /// <summary>
    /// 按名称取租户, 不存在抛 <see cref="SnowTenantNotFoundException"/>
    /// </summary>
    /// <param name="name">租户名称</param>
    /// <returns>租户配置</returns>
    Task<TenantConfiguration> GetAsync(string name);
}
