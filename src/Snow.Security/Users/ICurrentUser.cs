using System.Security.Claims;

namespace Snow.Security.Users;

/// <summary>
/// 当前登录用户(从 <see cref="Claims.ICurrentPrincipalAccessor"/> 的当前主体读取, 与认证方案无关)
/// <para>只保留实际会被消费的成员: 身份信息 + 角色 + 声明访问。</para>
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// 是否已认证(等价于 <see cref="Id"/> 有值)
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 用户Id
    /// </summary>
    long? Id { get; }

    /// <summary>
    /// 账号
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// 用户名称
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// 所属租户 id, 宿主用户为 null
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// 手机号
    /// </summary>
    string? PhoneNumber { get; }

    /// <summary>
    /// 邮箱
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// 角色列表
    /// </summary>
    string[] Roles { get; }

    /// <summary>
    /// 查找第一条指定类型的声明
    /// </summary>
    /// <param name="claimType">声明类型</param>
    /// <returns>声明, 不存在为 null</returns>
    Claim? FindClaim(string claimType);

    /// <summary>
    /// 取全部声明
    /// </summary>
    /// <returns>声明数组</returns>
    Claim[] GetAllClaims();

    /// <summary>
    /// 是否属于某角色
    /// </summary>
    /// <param name="roleName">角色名</param>
    /// <returns>是否属于</returns>
    bool IsInRole(string roleName);
}
