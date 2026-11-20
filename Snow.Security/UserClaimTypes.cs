using System.Security.Claims;

namespace Snow.Security;

/// <summary>
/// 当前用户各字段在声明集合中的键名, 对齐 ABP 的 <c>AbpClaimTypes</c>:
/// 能用 BCL 标准声明的用 <see cref="ClaimTypes"/> 长名, ABP 有约定的短名沿用其字面值, 业务字段用自定义短名。
/// <para>与 AbpClaimTypes 一致用可写属性而非常量, 便于宿主在启动早期统一替换。</para>
/// </summary>
public static class UserClaimTypes
{
    /// <summary>
    /// 登录账号。默认 <see cref="ClaimTypes.Name"/>
    /// </summary>
    public static string UserName { get; set; } = ClaimTypes.Name;

    /// <summary>
    /// 用户名称。默认 <see cref="ClaimTypes.GivenName"/>
    /// </summary>
    public static string Name { get; set; } = ClaimTypes.GivenName;

    /// <summary>
    /// 姓。默认 <see cref="ClaimTypes.Surname"/>
    /// </summary>
    public static string SurName { get; set; } = ClaimTypes.Surname;

    /// <summary>
    /// 用户Id。默认 <see cref="ClaimTypes.NameIdentifier"/>
    /// </summary>
    public static string UserId { get; set; } = ClaimTypes.NameIdentifier;

    /// <summary>
    /// 角色(可重复出现)。默认 <see cref="ClaimTypes.Role"/>
    /// </summary>
    public static string Role { get; set; } = ClaimTypes.Role;

    /// <summary>
    /// 邮箱。默认 <see cref="ClaimTypes.Email"/>
    /// </summary>
    public static string Email { get; set; } = ClaimTypes.Email;

    /// <summary>
    /// 邮箱已验证。默认 "email_verified"
    /// </summary>
    public static string EmailVerified { get; set; } = "email_verified";

    /// <summary>
    /// 手机号。默认 "phone_number"
    /// </summary>
    public static string PhoneNumber { get; set; } = "phone_number";

    /// <summary>
    /// 手机号已验证。默认 "phone_number_verified"
    /// </summary>
    public static string PhoneNumberVerified { get; set; } = "phone_number_verified";

    /// <summary>
    /// 租户Id。默认 "tenantid"
    /// </summary>
    public static string TenantId { get; set; } = "tenantid";

    /// <summary>
    /// 客户端Id。默认 "client_id"
    /// </summary>
    public static string ClientId { get; set; } = "client_id";

    /// <summary>
    /// 头像。默认 "picture"
    /// </summary>
    public static string Picture { get; set; } = "picture";

    /// <summary>
    /// 记住我。默认 "remember_me"
    /// </summary>
    public static string RememberMe { get; set; } = "remember_me";

    /// <summary>
    /// 会话Id。默认 "session_id"
    /// </summary>
    public static string SessionId { get; set; } = "session_id";

    /// <summary>
    /// 权限标识(可重复出现), 如 order:add —— 业务扩展, ABP 无对应项
    /// </summary>
    public static string Permission { get; set; } = "permission";

    /// <summary>
    /// 是否超级管理员(取值 true/false) —— 业务扩展
    /// </summary>
    public static string IsSuperAdmin { get; set; } = "is_super_admin";

    /// <summary>
    /// 菜单Id(可重复出现) —— 业务扩展
    /// </summary>
    public static string MenuId { get; set; } = "menu_id";

    /// <summary>
    /// 许可证(可重复出现) —— 业务扩展
    /// </summary>
    public static string License { get; set; } = "license";

    /// <summary>
    /// 公司Id(可重复出现) —— 业务扩展
    /// </summary>
    public static string CompanyId { get; set; } = "company_id";

    /// <summary>
    /// 部门Id(可重复出现) —— 业务扩展
    /// </summary>
    public static string DepartmentId { get; set; } = "department_id";

    /// <summary>
    /// 岗位Id(可重复出现) —— 业务扩展
    /// </summary>
    public static string PositionId { get; set; } = "position_id";
}
