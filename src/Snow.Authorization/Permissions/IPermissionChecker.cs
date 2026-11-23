using System.Security.Claims;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限校验器: 判断某个身份是否拥有权限
/// <para>默认实现 <see cref="ClaimsPermissionChecker"/> 读身份里的 permission 声明, 框架不保存授予数据;
/// 想按请求查库(权限改动立刻生效)就实现本接口并登记。</para>
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// 当前请求用户是否拥有指定权限
    /// </summary>
    /// <param name="name">权限名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有</returns>
    Task<bool> IsGrantedAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定身份是否拥有指定权限
    /// </summary>
    /// <param name="principal">身份</param>
    /// <param name="name">权限名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有</returns>
    Task<bool> IsGrantedAsync(ClaimsPrincipal? principal, string name, CancellationToken cancellationToken = default);
}
