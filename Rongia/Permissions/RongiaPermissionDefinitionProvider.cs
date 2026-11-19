using Rongia.Authorization.Permissions;

namespace Rongia.Permissions;

/// <summary>
/// 宿主权限定义: 声明本应用的权限树
/// <para>新增业务权限时在此登记, 之后即可用 <c>[RequirePermission("Rongia.Orders.Create")]</c> 或
/// <c>RequireAuthorization("Rongia.Orders.Create")</c> 声明式校验。</para>
/// </summary>
public sealed class RongiaPermissionDefinitionProvider : PermissionDefinitionProvider
{
    /// <inheritdoc />
    public override void Define(IPermissionDefinitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        PermissionGroupDefinition ordersGroup = context.AddGroup("Rongia.Orders", "订单管理");

        PermissionDefinition orders = ordersGroup.AddPermission("Rongia.Orders", "订单");
        orders.AddChild("Rongia.Orders.Create", "新建订单");
        orders.AddChild("Rongia.Orders.Update", "修改订单");
        orders.AddChild("Rongia.Orders.Delete", "删除订单");
        orders.AddChild("Rongia.Orders.Manage", "订单管理");

        PermissionGroupDefinition systemGroup = context.AddGroup("Rongia.System", "系统管理");
        PermissionDefinition users = systemGroup.AddPermission("Rongia.System.Users", "用户管理");
        users.AddChild("Rongia.System.Users.Create", "新建用户");
        users.AddChild("Rongia.System.Users.Delete", "删除用户");
    }
}
