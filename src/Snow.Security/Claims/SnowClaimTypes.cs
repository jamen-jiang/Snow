using System.Security.Claims;

namespace Snow.Security.Claims;

/// <summary>
/// 当前用户各字段在声明集合中的键名, 对齐 ABP 的 <c>AbpClaimTypes</c>:
/// 能用 BCL 标准声明的用 <see cref="ClaimTypes"/> 长名, 其余沿用 OIDC 与 ABP 的短名字面值。
/// <para>与 AbpClaimTypes 一致用可写属性而非常量, 便于宿主在启动早期统一替换。</para>
/// </summary>
public static class SnowClaimTypes
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
    /// 所属租户 id。默认 "tenantid"(与 ABP 的 AbpClaimTypes.TenantId 同名字面值)
    /// </summary>
    public static string TenantId { get; set; } = "tenantid";

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
    /// 手机号。默认 "phone_number"(OIDC 标准声明名, 沿用其字面值便于和外部令牌对接)
    /// </summary>
    public static string MobilePhone { get; set; } = "phone_number";

    /// <summary>
    /// 手机号已验证。默认 "phone_number_verified"
    /// </summary>
    public static string PhoneNumberVerified { get; set; } = "phone_number_verified";
}