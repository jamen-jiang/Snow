using Snow.DependencyInjection;
using Snow.Security;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 令牌声明提供器: 令牌里直接带 permission 声明时按声明放行(提供器名 P)
/// <para>兼容"把权限写进令牌"的旧用法; 权限写进令牌后改动需重新登录才生效。</para>
/// </summary>
public sealed class ClaimsPermissionValueProvider : IPermissionValueProvider, ITransientDependency
{
    /// <summary>
    /// 提供器名
    /// </summary>
    public const string ProviderName = "P";

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool contains = context.Principal?.FindAll(UserClaimTypes.Permission)
            .Any(claim => string.Equals(claim.Value, context.Permission.Name, StringComparison.Ordinal)) == true;

        return Task.FromResult(contains ? PermissionGrantResult.Granted : PermissionGrantResult.Undefined);
    }

    /// <inheritdoc />
    public Task<MultiplePermissionGrantResult> CheckAsync(PermissionValuesCheckContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HashSet<string> claimPermissions = context.Principal?.FindAll(UserClaimTypes.Permission)
            .Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal) ?? [];

        MultiplePermissionGrantResult result = new();
        foreach (PermissionDefinition permission in context.Permissions)
        {
            result.Result[permission.Name] = claimPermissions.Contains(permission.Name)
                ? PermissionGrantResult.Granted
                : PermissionGrantResult.Undefined;
        }

        return Task.FromResult(result);
    }
}
