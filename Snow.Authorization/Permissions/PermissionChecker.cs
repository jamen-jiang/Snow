using System.Security.Claims;
using Snow.DependencyInjection;
using Snow.Security.Claims;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限校验器默认实现: 定义校验(存在/启用/提供器限制) + 多值提供器三态聚合
/// </summary>
[TransientService]
public sealed class PermissionChecker : IPermissionChecker
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionValueProviderManager _permissionValueProviderManager;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionDefinitionManager">权限定义管理器</param>
    /// <param name="permissionValueProviderManager">权限值提供器管理器</param>
    /// <param name="principalAccessor">当前主体访问器</param>
    public PermissionChecker(
        IPermissionDefinitionManager permissionDefinitionManager,
        IPermissionValueProviderManager permissionValueProviderManager,
        ICurrentPrincipalAccessor principalAccessor)
    {
        ArgumentNullException.ThrowIfNull(permissionDefinitionManager);
        ArgumentNullException.ThrowIfNull(permissionValueProviderManager);
        ArgumentNullException.ThrowIfNull(principalAccessor);

        _permissionDefinitionManager = permissionDefinitionManager;
        _permissionValueProviderManager = permissionValueProviderManager;
        _principalAccessor = principalAccessor;
    }

    private ClaimsPrincipal? CurrentPrincipal => _principalAccessor.Principal;

    /// <inheritdoc />
    public Task<bool> IsGrantedAsync(string name, CancellationToken cancellationToken = default)
        => IsGrantedAsync(CurrentPrincipal, name);

    /// <inheritdoc />
    public async Task<bool> IsGrantedAsync(ClaimsPrincipal? principal, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        PermissionDefinition? permission = await _permissionDefinitionManager.GetOrNullAsync(name);
        if (permission == null || !permission.IsEnabled)
        {
            return false;
        }

        bool isGranted = false;
        PermissionValueCheckContext context = new(permission, principal);

        foreach (IPermissionValueProvider provider in _permissionValueProviderManager.ValueProviders)
        {
            if (permission.Providers.Count > 0 && !permission.Providers.Contains(provider.Name))
            {
                continue;
            }

            PermissionGrantResult result = await provider.CheckAsync(context);

            if (result == PermissionGrantResult.Granted)
            {
                isGranted = true;
            }
            else if (result == PermissionGrantResult.Prohibited)
            {
                return false;
            }
        }

        return isGranted;
    }

    /// <inheritdoc />
    public Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] names, CancellationToken cancellationToken = default)
        => IsGrantedAsync(CurrentPrincipal, names);

    /// <inheritdoc />
    public async Task<MultiplePermissionGrantResult> IsGrantedAsync(ClaimsPrincipal? principal, string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        MultiplePermissionGrantResult result = new();
        if (names.Length == 0)
        {
            return result;
        }

        List<PermissionDefinition> permissions = [];
        foreach (string name in names.Distinct(StringComparer.Ordinal))
        {
            PermissionDefinition? permission = await _permissionDefinitionManager.GetOrNullAsync(name);
            if (permission == null || !permission.IsEnabled)
            {
                result.Result[name] = PermissionGrantResult.Prohibited;
                continue;
            }

            result.Result[name] = PermissionGrantResult.Undefined;
            permissions.Add(permission);
        }

        foreach (IPermissionValueProvider provider in _permissionValueProviderManager.ValueProviders)
        {
            List<PermissionDefinition> pending = [.. permissions.Where(permission =>
                result[permission.Name] == PermissionGrantResult.Undefined &&
                (permission.Providers.Count == 0 || permission.Providers.Contains(provider.Name)))];

            if (pending.Count == 0)
            {
                continue;
            }

            MultiplePermissionGrantResult providerResult = await provider.CheckAsync(
                new PermissionValuesCheckContext(pending, principal));

            foreach (KeyValuePair<string, PermissionGrantResult> item in providerResult.Result)
            {
                if (item.Value != PermissionGrantResult.Undefined && result.Result.ContainsKey(item.Key))
                {
                    result.Result[item.Key] = item.Value;
                }
            }
        }

        return result;
    }
}
