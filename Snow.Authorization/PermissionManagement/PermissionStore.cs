using Snow.Authorization.Permissions;
using Snow.DependencyInjection;

namespace Snow.PermissionManagement;

/// <summary>
/// 权限存储实现: 从 <see cref="IPermissionGrantRepository"/> 读取授予数据
/// </summary>
public sealed class PermissionStore : IPermissionStore, ITransientDependency
{
    private readonly IPermissionGrantRepository _permissionGrantRepository;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionGrantRepository">权限授予仓储</param>
    public PermissionStore(IPermissionGrantRepository permissionGrantRepository)
    {
        ArgumentNullException.ThrowIfNull(permissionGrantRepository);
        _permissionGrantRepository = permissionGrantRepository;
    }

    /// <inheritdoc />
    public async Task<bool> IsGrantedAsync(string name, string? providerName, string? providerKey)
    {
        if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(providerKey))
        {
            return false;
        }

        return await _permissionGrantRepository.FindAsync(name, providerName, providerKey) != null;
    }

    /// <inheritdoc />
    public async Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] names, string? providerName, string? providerKey)
    {
        ArgumentNullException.ThrowIfNull(names);

        MultiplePermissionGrantResult result = new(names);
        if (names.Length == 0 || string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(providerKey))
        {
            return result;
        }

        List<PermissionGrant> grants = await _permissionGrantRepository.GetListAsync(providerName, providerKey, names);
        HashSet<string> grantedNames = [.. grants.Select(grant => grant.Name)];

        foreach (string name in names.Distinct(StringComparer.Ordinal))
        {
            result.Result[name] = grantedNames.Contains(name)
                ? PermissionGrantResult.Granted
                : PermissionGrantResult.Undefined;
        }

        return result;
    }
}
