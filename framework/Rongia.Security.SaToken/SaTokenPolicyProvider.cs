using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Rongia.Authorization;
using Rongia.Authorization.Permissions;

namespace Rongia.Security.SaToken;

/// <summary>
/// 策略提供器: 把 <c>satoken:login</c> / <c>satoken:permission:...</c> / <c>satoken:role:...</c> 动态翻译成 <see cref="SaTokenRequirement"/>
/// <para>其余策略名(含按权限名与 <c>permission:</c> 前缀的动态策略)原样交给 <see cref="RongiaAuthorizationPolicyProvider"/> 处理。</para>
/// </summary>
public sealed class SaTokenPolicyProvider : IAuthorizationPolicyProvider
{
    /// <summary>
    /// 登录策略名
    /// </summary>
    public const string LoginPolicy = "satoken:login";

    /// <summary>
    /// 权限策略名前缀
    /// </summary>
    public const string PermissionPolicyPrefix = "satoken:permission:";

    /// <summary>
    /// 角色策略名前缀
    /// </summary>
    public const string RolePolicyPrefix = "satoken:role:";

    private readonly RongiaAuthorizationPolicyProvider _fallback;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">授权选项</param>
    /// <param name="permissionDefinitionManager">权限定义管理器</param>
    public SaTokenPolicyProvider(IOptions<AuthorizationOptions> options, IPermissionDefinitionManager permissionDefinitionManager)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(permissionDefinitionManager);
        _fallback = new RongiaAuthorizationPolicyProvider(options, permissionDefinitionManager);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.Equals(policyName, LoginPolicy, StringComparison.Ordinal))
        {
            return Task.FromResult<AuthorizationPolicy?>(Build(new SaTokenRequirement(SaTokenCheckKind.Login)));
        }

        if (policyName.StartsWith(PermissionPolicyPrefix, StringComparison.Ordinal))
        {
            string[] permissions = SplitValues(policyName[PermissionPolicyPrefix.Length..]);
            return Task.FromResult<AuthorizationPolicy?>(Build(new SaTokenRequirement(SaTokenCheckKind.Permission, permissions)));
        }

        if (policyName.StartsWith(RolePolicyPrefix, StringComparison.Ordinal))
        {
            string[] roles = SplitValues(policyName[RolePolicyPrefix.Length..]);
            return Task.FromResult<AuthorizationPolicy?>(Build(new SaTokenRequirement(SaTokenCheckKind.Role, roles)));
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    private static AuthorizationPolicy Build(SaTokenRequirement requirement)
        => new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddRequirements(requirement).Build();

    private static string[] SplitValues(string raw)
        => raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
