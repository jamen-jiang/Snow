namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限存储: 读取某个(提供器, 提供器键)维度上是否被授予了某权限
/// <para>实现由权限管理包提供(当前为内存实现, 接入 EF Core 后换成数据库实现)。</para>
/// </summary>
public interface IPermissionStore
{
    /// <summary>
    /// 单个权限是否已授予
    /// </summary>
    /// <param name="name">权限名</param>
    /// <param name="providerName">提供器名, 如 U / R / C</param>
    /// <param name="providerKey">提供器键, 如 用户Id / 角色名 / 客户端Id</param>
    Task<bool> IsGrantedAsync(string name, string? providerName, string? providerKey);

    /// <summary>
    /// 批量权限是否已授予
    /// </summary>
    /// <param name="names">权限名集合</param>
    /// <param name="providerName">提供器名</param>
    /// <param name="providerKey">提供器键</param>
    Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] names, string? providerName, string? providerKey);
}
