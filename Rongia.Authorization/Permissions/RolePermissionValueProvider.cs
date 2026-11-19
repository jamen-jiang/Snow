using Rongia.Security;

namespace Rongia.Authorization.Permissions;

/// <summary>
/// 角色维度权限提供器: 任一角色被授予即通过(提供器名 R)
/// </summary>
public sealed class RolePermissionValueProvider : PermissionValueProvider
{
    /// <summary>
    /// 提供器名
    /// </summary>
    public const string ProviderName = "R";

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionStore">权限存储</param>
    public RolePermissionValueProvider(IPermissionStore permissionStore)
        : base(permissionStore)
    {
    }

    /// <inheritdoc />
    public override string Name => ProviderName;

    /// <inheritdoc />
    public override async Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        List<string> roles = context.Principal?.FindAll(UserClaimTypes.Role).Select(claim => claim.Value)
            .Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal).ToList() ?? [];

        foreach (string role in roles)
        {
            if (await PermissionStore.IsGrantedAsync(context.Permission.Name, Name, role))
            {
                return PermissionGrantResult.Granted;
            }
        }

        return PermissionGrantResult.Undefined;
    }

    /// <inheritdoc />
    public override async Task<MultiplePermissionGrantResult> CheckAsync(PermissionValuesCheckContext context)
    {
        string[] names = [.. context.Permissions.Select(permission => permission.Name).Distinct(StringComparer.Ordinal)];
        MultiplePermissionGrantResult result = new(names);

        List<string> roles = context.Principal?.FindAll(UserClaimTypes.Role).Select(claim => claim.Value)
            .Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal).ToList() ?? [];

        foreach (string role in roles)
        {
            MultiplePermissionGrantResult roleResult = await PermissionStore.IsGrantedAsync(names, Name, role);
            foreach (KeyValuePair<string, PermissionGrantResult> item in roleResult.Result)
            {
                if (item.Value == PermissionGrantResult.Granted && result.Result.ContainsKey(item.Key))
                {
                    result.Result[item.Key] = PermissionGrantResult.Granted;
                }
            }
        }

        return result;
    }
}
