using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Snow.Authorization;
using Snow.DependencyInjection;

namespace Snow.Authentication;

/// <summary>
/// 策略提供器: 把 <c>snowauth:login</c> / <c>snowauth:role:...</c> 动态翻译成 <see cref="TokenRequirement"/>
/// <para>其余策略名(即权限名)原样交给 <see cref="SnowAuthorizationPolicyProvider"/> 处理,
/// 复用容器里的同一个实例, 于是两套写法共用同一份策略缓存。</para>
/// </summary>
[SingletonService(ReplaceServices = true)]
[ExposeServices(typeof(IAuthorizationPolicyProvider))]
public sealed class TokenPolicyProvider : IAuthorizationPolicyProvider
{
    /// <summary>
    /// 登录策略名
    /// </summary>
    public const string LoginPolicy = "snowauth:login";

    /// <summary>
    /// 角色策略名前缀
    /// </summary>
    public const string RolePolicyPrefix = "snowauth:role:";

    private readonly SnowAuthorizationPolicyProvider _fallback;

    // 策略只由策略名决定, 与请求无关, 可以缓存; 与 SnowAuthorizationPolicyProvider 的缓存口径一致
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="fallback">权限策略提供器(由 <c>SnowAuthorizationModule</c> 按具体类型登记的单例)</param>
    public TokenPolicyProvider(SnowAuthorizationPolicyProvider fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        _fallback = fallback;
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    /// <inheritdoc />
    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        if (_cache.TryGetValue(policyName, out AuthorizationPolicy? cached))
        {
            return cached;
        }

        TokenRequirement? requirement = Resolve(policyName);
        if (requirement is null)
        {
            return await _fallback.GetPolicyAsync(policyName);
        }

        // 宿主显式 AddPolicy 注册了同名策略时以宿主的为准, 不被这里的动态策略盖掉
        AuthorizationPolicy? explicitPolicy = await _fallback.GetPolicyAsync(policyName);
        if (explicitPolicy is not null)
        {
            return explicitPolicy;
        }

        return _cache.GetOrAdd(policyName, _ => Build(requirement));
    }

    private static TokenRequirement? Resolve(string policyName)
    {
        if (string.Equals(policyName, LoginPolicy, StringComparison.Ordinal))
        {
            return new TokenRequirement(TokenCheckKind.Login);
        }

        if (policyName.StartsWith(RolePolicyPrefix, StringComparison.Ordinal))
        {
            return new TokenRequirement(TokenCheckKind.Role, SplitValues(policyName[RolePolicyPrefix.Length..]));
        }

        return null;
    }

    private static AuthorizationPolicy Build(TokenRequirement requirement)
        => new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddRequirements(requirement).Build();

    private static string[] SplitValues(string raw)
        => raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>
/// 令牌鉴权处理器: 登录看是否已认证, 角色看角色声明
/// <para>权限校验由 <c>Snow.Authorization</c> 的 <c>PermissionRequirementHandler</c> 负责, 这里不再重复实现。</para>
/// </summary>
[ScopedService]
[ExposeServices(typeof(IAuthorizationHandler))]
public sealed class TokenRequirementHandler : AuthorizationHandler<TokenRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TokenRequirement requirement)
    {
        switch (requirement.Kind)
        {
            case TokenCheckKind.Login:
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    context.Succeed(requirement);
                }

                break;

            case TokenCheckKind.Role:
                if (requirement.Values.Any(role => context.User.IsInRole(role)))
                {
                    context.Succeed(requirement);
                }

                break;
        }

        return Task.CompletedTask;
    }
}
