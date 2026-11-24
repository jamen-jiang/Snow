using Snow.DependencyInjection;

namespace Snow.MultiTenancy;

/// <summary>
/// 空的租户存储: 一个租户都没有
/// <para>框架默认实现, 让未接入租户表的应用也能解析 <see cref="ITenantStore"/>。</para>
/// </summary>
[SingletonService]
public sealed class NullTenantStore : ITenantStore
{
    /// <inheritdoc />
    /// <param name="id">标识</param>
    /// <param name="name">名称</param>
    public Task<TenantConfiguration?> FindAsync(Guid? id, string? name = null) => Task.FromResult<TenantConfiguration?>(null);

    /// <inheritdoc />
    /// <param name="id">标识</param>
    public Task<TenantConfiguration?> FindAsync(Guid id) => Task.FromResult<TenantConfiguration?>(null);

    /// <inheritdoc />
    /// <param name="name">名称</param>
    public Task<TenantConfiguration?> FindAsync(string name) => Task.FromResult<TenantConfiguration?>(null);

    /// <inheritdoc />
    /// <param name="id">标识</param>
    public Task<TenantConfiguration> GetAsync(Guid id) => throw new SnowTenantNotFoundException(id, null);

    /// <inheritdoc />
    /// <param name="name">名称</param>
    public Task<TenantConfiguration> GetAsync(string name) => throw new SnowTenantNotFoundException(null, name);
}
