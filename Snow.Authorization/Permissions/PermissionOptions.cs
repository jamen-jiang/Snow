namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限选项: 登记权限定义提供器与权限值提供器
/// </summary>
public sealed class PermissionOptions
{
    /// <summary>
    /// 权限定义提供器类型集合
    /// </summary>
    public TypeList DefinitionProviders { get; } = [];

    /// <summary>
    /// 权限值提供器类型集合
    /// </summary>
    public TypeList ValueProviders { get; } = [];
}
