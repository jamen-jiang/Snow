using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Snow.Authorization.Permissions;

namespace Snow.Authorization;

/// <summary>
/// 权限策略提供器: 把"权限名"直接当策略名使用, 动态构造 <see cref="PermissionRequirement"/>
/// <para>对齐 ABP 的 AbpAuthorizationPolicyProvider: 先查显式注册的策略, 再查权限定义, 最后回落默认提供器。</para>
/// </summary>
public sealed class SnowAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">授权选项</param>
    /// <param name="permissionDefinitionManager">权限定义管理器</param>
    public SnowAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options,
        IPermissionDefinitionManager permissionDefinitionManager)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(permissionDefinitionManager);

        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        _permissionDefinitionManager = permissionDefinitionManager;
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    /// <inheritdoc />
    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        AuthorizationPolicy? policy = await _fallbackPolicyProvider.GetPolicyAsync(policyName);
        if (policy != null)
        {
            return policy;
        }

        string permissionName = policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal)
            ? policyName[RequirePermissionAttribute.PolicyPrefix.Length..]
            : policyName;

        if (await _permissionDefinitionManager.GetOrNullAsync(permissionName) == null)
        {
            return null;
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissionName))
            .Build();
    }
}
