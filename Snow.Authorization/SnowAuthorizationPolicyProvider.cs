using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Snow.Authorization.Permissions;
using Snow.DependencyInjection;

namespace Snow.Authorization;

/// <summary>
/// 权限策略提供器: 把"权限名"直接当策略名使用, 动态构造 <see cref="PermissionRequirement"/>
/// <para>对齐 ABP 的 AbpAuthorizationPolicyProvider: 先查显式注册的策略, 再查权限定义。</para>
/// <para>动态策略构造一次后缓存: 同一权限在整条请求链上只会 new 一次 <see cref="AuthorizationPolicy"/>。
/// 只缓存命中的策略, 不缓存 null —— 否则权限定义变更后, 之前查不到的策略会一直查不到。</para>
/// </summary>
[SingletonService(ReplaceServices = true)]
public sealed class SnowAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new(StringComparer.Ordinal);

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
        if (_cache.TryGetValue(policyName, out AuthorizationPolicy? cached))
        {
            return cached;
        }

        // 显式注册的策略优先, 这类策略由 AuthorizationOptions 管理, 不进本提供器的缓存
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

        AuthorizationPolicy dynamicPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissionName))
            .Build();

        return _cache.GetOrAdd(policyName, dynamicPolicy);
    }
}
