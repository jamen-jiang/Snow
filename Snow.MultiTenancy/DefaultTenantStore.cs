using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.MultiTenancy;

/// <summary>
/// 默认租户存储: 从 <see cref="SnowDefaultTenantStoreOptions"/> 里查, 顶替空的租户存储
/// <para>名称比较用不区分大小写的规范化值(去首尾空白后转小写), 停用的租户当作不存在。</para>
/// </summary>
[SingletonService(ReplaceServices = true)]
public sealed class DefaultTenantStore : ITenantStore
{
    private readonly SnowDefaultTenantStoreOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">租户清单选项</param>
    public DefaultTenantStore(IOptionsMonitor<SnowDefaultTenantStoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.CurrentValue;
    }

    /// <inheritdoc />
    public Task<TenantConfiguration?> FindAsync(Guid? id, string? name = null)
    {
        if (id.HasValue)
        {
            return Task.FromResult(FindById(id.Value));
        }

        return string.IsNullOrWhiteSpace(name)
            ? Task.FromResult<TenantConfiguration?>(null)
            : Task.FromResult(FindByName(name));
    }

    /// <inheritdoc />
    public Task<TenantConfiguration?> FindAsync(Guid id) => Task.FromResult(FindById(id));

    /// <inheritdoc />
    public Task<TenantConfiguration?> FindAsync(string name) => Task.FromResult(FindByName(name));

    /// <inheritdoc />
    public async Task<TenantConfiguration> GetAsync(Guid id)
    {
        TenantConfiguration? tenant = await FindAsync(id).ConfigureAwait(false);
        return tenant ?? throw new SnowTenantNotFoundException(id, null);
    }

    /// <inheritdoc />
    public async Task<TenantConfiguration> GetAsync(string name)
    {
        TenantConfiguration? tenant = await FindAsync(name).ConfigureAwait(false);
        return tenant ?? throw new SnowTenantNotFoundException(null, name);
    }

    private TenantConfiguration? FindById(Guid id)
        => _options.Tenants.FirstOrDefault(tenant => tenant.Id == id && tenant.IsActive);

    private TenantConfiguration? FindByName(string name)
    {
        string normalized = NormalizeName(name);
        return _options.Tenants.FirstOrDefault(tenant =>
            tenant.IsActive && string.Equals(NormalizeName(tenant.Name), normalized, StringComparison.Ordinal));
    }

    private static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
}
