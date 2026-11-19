using Rongia.Security;

namespace Rongia.Authorization.Permissions;

/// <summary>
/// 用户维度权限提供器: 按当前用户Id查授权(提供器名 U)
/// </summary>
public sealed class UserPermissionValueProvider : PermissionValueProvider
{
    /// <summary>
    /// 提供器名
    /// </summary>
    public const string ProviderName = "U";

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionStore">权限存储</param>
    public UserPermissionValueProvider(IPermissionStore permissionStore)
        : base(permissionStore)
    {
    }

    /// <inheritdoc />
    public override string Name => ProviderName;

    /// <inheritdoc />
    public override async Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        string? userId = context.Principal?.FindFirst(UserClaimTypes.UserId)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return PermissionGrantResult.Undefined;
        }

        return await PermissionStore.IsGrantedAsync(context.Permission.Name, Name, userId)
            ? PermissionGrantResult.Granted
            : PermissionGrantResult.Undefined;
    }

    /// <inheritdoc />
    public override async Task<MultiplePermissionGrantResult> CheckAsync(PermissionValuesCheckContext context)
    {
        string[] names = [.. context.Permissions.Select(permission => permission.Name).Distinct(StringComparer.Ordinal)];
        string? userId = context.Principal?.FindFirst(UserClaimTypes.UserId)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return new MultiplePermissionGrantResult(names);
        }

        return await PermissionStore.IsGrantedAsync(names, Name, userId);
    }
}
