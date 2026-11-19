namespace Rongia.Authorization.Permissions;

/// <summary>
/// 权限定义管理器: 汇总全部定义提供器的结果并按名称索引
/// </summary>
public interface IPermissionDefinitionManager
{
    /// <summary>
    /// 按名称取权限, 不存在抛异常
    /// </summary>
    /// <param name="name">权限名</param>
    Task<PermissionDefinition> GetAsync(string name);

    /// <summary>
    /// 按名称取权限, 不存在返回 null
    /// </summary>
    /// <param name="name">权限名</param>
    Task<PermissionDefinition?> GetOrNullAsync(string name);

    /// <summary>
    /// 取全部权限(已展开树)
    /// </summary>
    Task<IReadOnlyList<PermissionDefinition>> GetPermissionsAsync();

    /// <summary>
    /// 取全部分组
    /// </summary>
    Task<IReadOnlyList<PermissionGroupDefinition>> GetGroupsAsync();
}
