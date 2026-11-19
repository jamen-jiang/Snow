using System.Collections.Concurrent;
using Rongia.DependencyInjection;

namespace Rongia.PermissionManagement;

/// <summary>
/// 权限授予仓储内存实现(进程内单例)
/// <para>接入 EF Core 后提供数据库实现即可, 通过 [Dependency(ReplaceServices = true)] 覆盖本实现。</para>
/// </summary>
public sealed class InMemoryPermissionGrantRepository : IPermissionGrantRepository, ISingletonDependency
{
    private readonly ConcurrentDictionary<(string Name, string ProviderName, string ProviderKey), PermissionGrant> _grants = new();

    /// <inheritdoc />
    public Task<PermissionGrant?> FindAsync(string name, string providerName, string providerKey)
    {
        _grants.TryGetValue((name, providerName, providerKey), out PermissionGrant? grant);
        return Task.FromResult(grant);
    }

    /// <inheritdoc />
    public Task<List<PermissionGrant>> GetListAsync(string providerName, string providerKey, IReadOnlyCollection<string>? names = null)
    {
        HashSet<string>? nameSet = names is { Count: > 0 } ? [.. names] : null;

        List<PermissionGrant> grants = [.. _grants.Values.Where(grant =>
            string.Equals(grant.ProviderName, providerName, StringComparison.Ordinal) &&
            string.Equals(grant.ProviderKey, providerKey, StringComparison.Ordinal) &&
            (nameSet == null || nameSet.Contains(grant.Name)))];

        return Task.FromResult(grants);
    }

    /// <inheritdoc />
    public Task InsertAsync(PermissionGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        _grants[(grant.Name, grant.ProviderName, grant.ProviderKey)] = grant;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string name, string providerName, string providerKey)
    {
        _grants.TryRemove((name, providerName, providerKey), out _);
        return Task.CompletedTask;
    }
}
