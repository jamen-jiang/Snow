namespace Snow.MultiTenancy;

/// <summary>
/// 默认租户存储的选项(对齐 ABP 的 <c>AbpDefaultTenantStoreOptions</c>)
/// <para>租户表直接写在配置里, 适合单机部署或开发环境; 生产环境换成读数据库的 <see cref="ITenantStore"/> 实现。</para>
/// <para>绑定配置根的 <c>Tenants</c> 数组(与 ABP 一样整体绑定根配置), 每项只需 <c>Id</c> 与 <c>Name</c>。</para>
/// </summary>
public sealed class SnowDefaultTenantStoreOptions
{
    /// <summary>
    /// 租户清单
    /// </summary>
    public List<TenantConfiguration> Tenants { get; set; } = [];
}
