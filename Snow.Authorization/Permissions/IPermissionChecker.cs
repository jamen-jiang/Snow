using System.Security.Claims;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限校验器: 判断某个身份是否拥有权限
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// 当前请求用户是否拥有指定权限
    /// </summary>
    /// <param name="name">权限名</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<bool> IsGrantedAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定身份是否拥有指定权限
    /// </summary>
    /// <param name="principal">身份</param>
    /// <param name="name">权限名</param>
    Task<bool> IsGrantedAsync(ClaimsPrincipal? principal, string name);

    /// <summary>
    /// 当前请求用户是否拥有指定的多个权限
    /// </summary>
    /// <param name="names">权限名集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] names, CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定身份是否拥有指定的多个权限
    /// </summary>
    /// <param name="principal">身份</param>
    /// <param name="names">权限名集合</param>
    Task<MultiplePermissionGrantResult> IsGrantedAsync(ClaimsPrincipal? principal, string[] names);
}
