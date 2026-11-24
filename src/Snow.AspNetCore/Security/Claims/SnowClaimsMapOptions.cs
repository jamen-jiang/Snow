using Snow.Security.Claims;
using Snow.Security;

namespace Snow.AspNetCore.Security.Claims;

/// <summary>
/// 声明名映射表: 外部令牌的声明名 → 本框架的声明名(对应 ABP 的 <c>AbpClaimsMapOptions</c>)
/// <para>默认表覆盖 OIDC 标准声明, 与 ABP 的六条一致; 值用 <c>Func&lt;string&gt;</c> 而非字符串,
/// 因为 <see cref="SnowClaimTypes"/> 的成员是可写属性, 宿主可能在启动早期改掉, 映射要跟着走。</para>
/// </summary>
public class SnowClaimsMapOptions
{
    /// <summary>
    /// 声明名映射表: 键是外部令牌里的声明名, 值是取本框架声明名的委托
    /// </summary>
    public Dictionary<string, Func<string>> Maps { get; } =
        new()
        {
            { "sub", () => SnowClaimTypes.UserId },
            { "role", () => SnowClaimTypes.Role },
            { "email", () => SnowClaimTypes.Email },
            { "name", () => SnowClaimTypes.UserName },
            { "family_name", () => SnowClaimTypes.SurName },
            { "given_name", () => SnowClaimTypes.Name },
        };
}
