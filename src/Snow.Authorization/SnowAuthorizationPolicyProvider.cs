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
/// <para>用法就是 <c>[Authorize("Snow.Orders.Create")]</c>: 策略名与权限定义里的名字逐字符相同, 且该权限必须已被某个
/// <see cref="PermissionDefinitionProvider"/> 定义。</para>
/// <para>多个权限有三种写法: <c>[Permission("A","B")]</c>(默认满足其一, 写 <c>Mode = PermissionMatchMode.All</c> 改成都要有)、
/// <c>[Authorize("A,B")]</c>(逗号, 满足其一)、叠两个 <c>[Authorize]</c> 特性(都要有, 那是 ASP.NET Core 合并多个特性的规则, 与本提供器无关)。</para>
/// </summary>
[SingletonService(ReplaceServices = true)]
public sealed class SnowAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;
    private readonly PermissionDefinitionManager _permissionDefinitionManager;
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">授权选项</param>
    /// <param name="permissionDefinitionManager">权限定义管理器</param>
    public SnowAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options,
        PermissionDefinitionManager permissionDefinitionManager)
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

        // 策略名有三种来源: [Permission("A","B")] 编出的 "perm-any:A,B" / "perm-all:A,B",
        // 以及手写 [Authorize("A")] 或 [Authorize("A,B")](逗号 = 满足其一)
        PermissionMatchMode mode = PermissionMatchMode.Any;
        string namesPart = policyName;
        if (policyName.StartsWith(PermissionAttribute.AllPolicyPrefix, StringComparison.Ordinal))
        {
            mode = PermissionMatchMode.All;
            namesPart = policyName[PermissionAttribute.AllPolicyPrefix.Length..];
        }
        else if (policyName.StartsWith(PermissionAttribute.AnyPolicyPrefix, StringComparison.Ordinal))
        {
            namesPart = policyName[PermissionAttribute.AnyPolicyPrefix.Length..];
        }

        string[] permissionNames = namesPart
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // 未定义的权限拿不到策略, 授权中间件会抛 InvalidOperationException(500), 名字写错不会静默放行。
        // 多个名字时任何一个没定义也照样抛: 拼错的权限名不该因为另一个是对的就被放过。
        foreach (string permissionName in permissionNames)
        {
            if (_permissionDefinitionManager.GetOrNull(permissionName) == null)
            {
                return null;
            }
        }

        AuthorizationPolicyBuilder builder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
        if (mode == PermissionMatchMode.All)
        {
            // 都要有: 每个权限一条要求, 全部通过才放行
            foreach (string permissionName in permissionNames)
            {
                builder.AddRequirements(new PermissionRequirement(permissionName));
            }
        }
        else
        {
            builder.AddRequirements(permissionNames.Length == 1
                ? new PermissionRequirement(permissionNames[0])
                : new AnyPermissionRequirement(permissionNames));
        }

        return _cache.GetOrAdd(policyName, builder.Build());
    }
}
