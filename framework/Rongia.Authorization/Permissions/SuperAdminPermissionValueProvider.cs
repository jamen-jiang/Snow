using Rongia.DependencyInjection;
using Rongia.Security;

namespace Rongia.Authorization.Permissions;

/// <summary>
/// 超级管理员提供器: 令牌带 is_super_admin=true 时对任意权限直接授予(提供器名 S)
/// </summary>
public sealed class SuperAdminPermissionValueProvider : IPermissionValueProvider, ITransientDependency
{
    /// <summary>
    /// 提供器名
    /// </summary>
    public const string ProviderName = "S";

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool isSuperAdmin = string.Equals(
            context.Principal?.FindFirst(UserClaimTypes.IsSuperAdmin)?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(isSuperAdmin ? PermissionGrantResult.Granted : PermissionGrantResult.Undefined);
    }

    /// <inheritdoc />
    public Task<MultiplePermissionGrantResult> CheckAsync(PermissionValuesCheckContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string[] names = [.. context.Permissions.Select(permission => permission.Name).Distinct(StringComparer.Ordinal)];
        bool isSuperAdmin = string.Equals(
            context.Principal?.FindFirst(UserClaimTypes.IsSuperAdmin)?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(new MultiplePermissionGrantResult(
            names,
            isSuperAdmin ? PermissionGrantResult.Granted : PermissionGrantResult.Undefined));
    }
}
