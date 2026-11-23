using System.Security.Claims;
using Snow.DependencyInjection;
using Snow.Security.Claims;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限校验器默认实现: 直接读身份里的权限声明, 框架不存授予数据
/// <para>登录签发令牌时把该用户拥有的权限名写进 <see cref="PermissionClaimType"/> 声明(可多条),
/// 校验就是一次声明比对 —— 没有存储、没有跨实例同步、重启不丢。</para>
/// <para>代价是权限变更要等令牌重新签发: 想让改动立刻生效就得用短令牌 + 刷新, 或者换掉本实现改为按请求查库。</para>
/// </summary>
[TransientService]
public sealed class ClaimsPermissionChecker : IPermissionChecker
{
    /// <summary>
    /// 权限声明的类型名; 一个身份可以有多条该声明, 每条一个权限名
    /// </summary>
    public const string PermissionClaimType = "permission";

    /// <summary>
    /// 通配权限: 拥有它等于拥有全部权限(给超级管理员用)
    /// </summary>
    public const string AllPermission = "*";

    private readonly PermissionDefinitionManager _definitions;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="definitions">权限定义管理器</param>
    /// <param name="principalAccessor">当前主体访问器</param>
    public ClaimsPermissionChecker(
        PermissionDefinitionManager definitions,
        ICurrentPrincipalAccessor principalAccessor)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(principalAccessor);

        _definitions = definitions;
        _principalAccessor = principalAccessor;
    }

    /// <inheritdoc />
    public Task<bool> IsGrantedAsync(string name, CancellationToken cancellationToken = default)
        => IsGrantedAsync(_principalAccessor.Principal, name, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsGrantedAsync(ClaimsPrincipal? principal, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // 未定义或已停用: 一律不通过。这一步是护栏, 让打错字的权限名在校验时就是"没有",
        // 而不是因为身份里恰好多了个同名声明就放行
        PermissionDefinition? definition = _definitions.GetOrNull(name);
        if (definition is null || !definition.IsEnabled || principal is null)
        {
            return Task.FromResult(false);
        }

        foreach (Claim claim in principal.FindAll(PermissionClaimType))
        {
            if (claim.Value == name || claim.Value == AllPermission)
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }
}
