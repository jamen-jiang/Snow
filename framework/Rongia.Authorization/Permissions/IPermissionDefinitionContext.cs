namespace Rongia.Authorization.Permissions;

/// <summary>
/// 权限定义上下文: 定义提供器通过它登记分组与权限
/// </summary>
public interface IPermissionDefinitionContext
{
    /// <summary>
    /// 服务提供器(供提供器按需取依赖)
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 增加分组
    /// </summary>
    /// <param name="name">分组名</param>
    /// <param name="displayName">显示名称</param>
    /// <returns>分组定义</returns>
    PermissionGroupDefinition AddGroup(string name, string? displayName = null);

    /// <summary>
    /// 取分组, 不存在抛异常
    /// </summary>
    /// <param name="name">分组名</param>
    PermissionGroupDefinition GetGroup(string name);

    /// <summary>
    /// 取分组, 不存在返回 null
    /// </summary>
    /// <param name="name">分组名</param>
    PermissionGroupDefinition? GetGroupOrNull(string name);
}
