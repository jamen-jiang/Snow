namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限值提供器: 从某个维度(用户/角色/客户端/超级管理员/令牌声明)给出权限判定
/// </summary>
public interface IPermissionValueProvider
{
    /// <summary>
    /// 提供器名(如 U / R / C / S / P), 全局唯一
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 单权限求值
    /// </summary>
    /// <param name="context">求值上下文</param>
    Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context);

    /// <summary>
    /// 多权限求值
    /// </summary>
    /// <param name="context">求值上下文</param>
    Task<MultiplePermissionGrantResult> CheckAsync(PermissionValuesCheckContext context);
}
