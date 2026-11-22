using Snow.Authorization.Permissions;
using Snow.DependencyInjection;

namespace Snow.Authorization.PermissionManagement;

/// <summary>
/// 权限管理器默认实现
/// </summary>
[TransientService]
public sealed class PermissionManager : IPermissionManager
{
    private readonly IPermissionGrantRepository _permissionGrantRepository;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionGrantRepository">权限授予仓储</param>
    public PermissionManager(IPermissionGrantRepository permissionGrantRepository)
    {
        ArgumentNullException.ThrowIfNull(permissionGrantRepository);
        _permissionGrantRepository = permissionGrantRepository;
    }

    /// <inheritdoc />
    public async Task<MultiplePermissionGrantResult> GetAsync(string providerName, string providerKey, string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        MultiplePermissionGrantResult result = new(names);
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

    /// <inheritdoc />
    public async Task SetAsync(string name, string providerName, string providerKey, bool isGranted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);

        PermissionGrant? existing = await _permissionGrantRepository.FindAsync(name, providerName, providerKey);

        if (isGranted)
        {
            if (existing == null)
            {
                await _permissionGrantRepository.InsertAsync(new PermissionGrant
                {
                    Name = name,
                    ProviderName = providerName,
                    ProviderKey = providerKey,
                });
            }

            return;
        }

        if (existing != null)
        {
            await _permissionGrantRepository.DeleteAsync(name, providerName, providerKey);
        }
    }
}
