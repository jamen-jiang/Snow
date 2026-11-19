using Rongia.Authorization.Permissions;

namespace Rongia.PermissionManagement;

/// <summary>
/// 权限管理器: 查询与变更某主体的权限授予
/// </summary>
public interface IPermissionManager
{
    /// <summary>
    /// 查询某主体在某提供器下的多个权限授予情况
    /// </summary>
    /// <param name="providerName">提供器名</param>
    /// <param name="providerKey">提供器键</param>
    /// <param name="names">权限名集合</param>
    Task<MultiplePermissionGrantResult> GetAsync(string providerName, string providerKey, string[] names);

    /// <summary>
    /// 授予或撤销某主体的单个权限
    /// </summary>
    /// <param name="name">权限名</param>
    /// <param name="providerName">提供器名</param>
    /// <param name="providerKey">提供器键</param>
    /// <param name="isGranted">true 授予, false 撤销</param>
    Task SetAsync(string name, string providerName, string providerKey, bool isGranted);
}
