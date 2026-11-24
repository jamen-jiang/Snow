using System.Security.Claims;
using Snow.Collections;

namespace Snow.Security.Claims;

/// <summary>
/// 声明主体工厂选项(对应 ABP 的 <c>AbpClaimsPrincipalFactoryOptions</c>)
/// </summary>
public class SnowClaimsPrincipalFactoryOptions
{
    /// <summary>
    /// 静态贡献者类型清单
    /// </summary>
    public ITypeList<ISnowClaimsPrincipalContributor> Contributors { get; }

    /// <summary>
    /// 动态贡献者类型清单
    /// </summary>
    public ITypeList<ISnowDynamicClaimsPrincipalContributor> DynamicContributors { get; }

    /// <summary>
    /// 需要动态刷新的声明类型
    /// </summary>
    public List<string> DynamicClaims { get; }

    /// <summary>
    /// 是否启用远程刷新
    /// </summary>
    public bool IsRemoteRefreshEnabled { get; set; }

    /// <summary>
    /// 远程刷新地址
    /// </summary>
    public string RemoteRefreshUrl { get; set; }

    /// <summary>
    /// 外部令牌声明名到 Snow 声明名的映射
    /// </summary>
    public Dictionary<string, List<string>> ClaimsMap { get; set; }

    /// <summary>
    /// 是否启用动态声明
    /// </summary>
    public bool IsDynamicClaimsEnabled { get; set; }

    /// <summary>
    /// 构造器: 填入默认动态声明与映射表
    /// </summary>
    public SnowClaimsPrincipalFactoryOptions()
    {
        Contributors = new TypeList<ISnowClaimsPrincipalContributor>();
        DynamicContributors = new TypeList<ISnowDynamicClaimsPrincipalContributor>();

        DynamicClaims = new List<string>
        {
            SnowClaimTypes.UserName,
            SnowClaimTypes.Name,
            SnowClaimTypes.SurName,
            SnowClaimTypes.Role,
            SnowClaimTypes.Email,
            SnowClaimTypes.EmailVerified,
            SnowClaimTypes.MobilePhone,
            SnowClaimTypes.PhoneNumberVerified
        };

        RemoteRefreshUrl = "/api/account/dynamic-claims/refresh";
        IsRemoteRefreshEnabled = true;

        ClaimsMap = new Dictionary<string, List<string>>
        {
            { SnowClaimTypes.UserName, new List<string> { "preferred_username", "unique_name", ClaimTypes.Name } },
            { SnowClaimTypes.Name, new List<string> { "given_name", ClaimTypes.GivenName } },
            { SnowClaimTypes.SurName, new List<string> { "family_name", ClaimTypes.Surname } },
            { SnowClaimTypes.Role, new List<string> { "role", "roles", ClaimTypes.Role } },
            { SnowClaimTypes.Email, new List<string> { "email", ClaimTypes.Email } }
        };

        IsDynamicClaimsEnabled = false;
    }
}
