namespace Rongia.Authorization.Permissions;

/// <summary>
/// 权限值提供器管理器
/// </summary>
public interface IPermissionValueProviderManager
{
    /// <summary>
    /// 全部已登记的值提供器(按登记顺序)
    /// </summary>
    IReadOnlyList<IPermissionValueProvider> ValueProviders { get; }
}
