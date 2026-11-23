using Microsoft.AspNetCore.Authorization;

namespace Snow.Authorization;

/// <summary>
/// 权限注解: 直接写权限名, 一个或多个都行 —— <c>[Permission("Snow.Orders.Create")]</c>、
/// <c>[Permission("Snow.Orders.Create", "Snow.Orders.Update")]</c>
/// <para>与 <c>[Authorize]</c> 的区别就是这里能一次写多个: 框架的 <see cref="AuthorizeAttribute"/> 只有无参与单参构造,
/// 写不出 <c>[Authorize("A", "B")]</c>; 而 <c>[Authorize("A,B")]</c> 这种逗号字符串又容易被当成一个名字。</para>
/// <para>多个权限默认"满足其一即可", 要"都要有"就写 <c>Mode = PermissionMatchMode.All</c>。</para>
/// <para>继承 <see cref="AuthorizeAttribute"/>, 所以 MVC 控制器与最小 API 的 <c>RequireAuthorization</c> 都能直接用。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// "任一"策略名前缀
    /// </summary>
    public const string AnyPolicyPrefix = "perm-any:";

    /// <summary>
    /// "全部"策略名前缀
    /// </summary>
    public const string AllPolicyPrefix = "perm-all:";

    private PermissionMatchMode _mode = PermissionMatchMode.Any;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissions">权限名, 至少一个</param>
    public PermissionAttribute(params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        if (permissions.Length == 0)
        {
            throw new ArgumentException("至少需要一个权限名。", nameof(permissions));
        }

        foreach (string permission in permissions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        }

        Permissions = permissions;
        ApplyPolicy();
    }

    /// <summary>
    /// 权限名列表
    /// </summary>
    public IReadOnlyList<string> Permissions { get; }

    /// <summary>
    /// 多个权限的满足方式, 默认 <see cref="PermissionMatchMode.Any"/>
    /// </summary>
    public PermissionMatchMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            ApplyPolicy();
        }
    }

    /// <summary>
    /// 把权限名列表编成策略名, 交给 <see cref="SnowAuthorizationPolicyProvider"/> 解析
    /// </summary>
    private void ApplyPolicy()
        => Policy = (_mode == PermissionMatchMode.All ? AllPolicyPrefix : AnyPolicyPrefix)
                    + string.Join(",", Permissions);
}
