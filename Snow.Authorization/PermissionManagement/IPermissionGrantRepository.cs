namespace Snow.PermissionManagement;

/// <summary>
/// 权限授予仓储
/// </summary>
public interface IPermissionGrantRepository
{
    /// <summary>
    /// 查找单条授予记录
    /// </summary>
    /// <param name="name">权限名</param>
    /// <param name="providerName">提供器名</param>
    /// <param name="providerKey">提供器键</param>
    Task<PermissionGrant?> FindAsync(string name, string providerName, string providerKey);

    /// <summary>
    /// 列出某主体在指定提供器下的授予记录
    /// </summary>
    /// <param name="providerName">提供器名</param>
    /// <param name="providerKey">提供器键</param>
    /// <param name="names">限定权限名(为空则不限)</param>
    Task<List<PermissionGrant>> GetListAsync(string providerName, string providerKey, IReadOnlyCollection<string>? names = null);

    /// <summary>
    /// 新增授予记录
    /// </summary>
    /// <param name="grant">授予记录</param>
    Task InsertAsync(PermissionGrant grant);

    /// <summary>
    /// 删除授予记录
    /// </summary>
    /// <param name="name">权限名</param>
    /// <param name="providerName">提供器名</param>
    /// <param name="providerKey">提供器键</param>
    Task DeleteAsync(string name, string providerName, string providerKey);
}
