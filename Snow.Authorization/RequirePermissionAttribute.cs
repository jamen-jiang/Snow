using Microsoft.AspNetCore.Authorization;

namespace Snow.Authorization;

/// <summary>
/// 声明式权限校验: 标在控制器/动作(或任意端点)上, 要求当前用户具备指定权限
/// <para>等价于 Sa-Token 的 <c>@SaCheckPermission("order:add")</c>。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 策略名前缀: 带前缀时不必先定义权限, 直接按名字求值
    /// </summary>
    public const string PolicyPrefix = "permission:";

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permission">权限名, 如 Snow.Orders.Create</param>
    public RequirePermissionAttribute(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
        Policy = PolicyPrefix + permission;
    }

    /// <summary>
    /// 权限名
    /// </summary>
    public string Permission { get; }
}
