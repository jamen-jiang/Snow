using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Snow.Security;

namespace Snow.AspNetCore.Security.Claims;

/// <summary>
/// 把外部令牌的标准声明名映射成本框架的声明名(对应 ABP 的 <c>AbpClaimsTransformation</c>)
/// <para>认证中间件在认证成功后调用一次: 命中 <see cref="SnowClaimsMapOptions.Maps"/> 的声明会被复制一份、
/// 换成映射后的类型名, 追加到同一个身份上。原声明保留, 所以两边都能读到。</para>
/// <para>由 <see cref="SnowAspNetCoreServiceCollectionExtensions.TransformSnowClaims"/> 登记, 不调就不生效。</para>
/// </summary>
public class SnowClaimsTransformation : IClaimsTransformation
{
    /// <summary>
    /// 声明名映射选项
    /// </summary>
    protected IOptions<SnowClaimsMapOptions> ClaimsMapOptions { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="claimsMapOptions">声明名映射选项</param>
    public SnowClaimsTransformation(IOptions<SnowClaimsMapOptions> claimsMapOptions)
    {
        ArgumentNullException.ThrowIfNull(claimsMapOptions);

        ClaimsMapOptions = claimsMapOptions;
    }

    /// <inheritdoc />
    public virtual Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        Dictionary<string, Func<string>> maps = ClaimsMapOptions.Value.Maps;
        List<Claim> mappedClaims = [];
        foreach (Claim claim in principal.Claims)
        {
            if (maps.TryGetValue(claim.Type, out Func<string>? targetType))
            {
                mappedClaims.Add(new Claim(targetType(), claim.Value, claim.ValueType, claim.Issuer));
            }
        }

        if (mappedClaims.Count > 0)
        {
            principal.AddIdentity(new ClaimsIdentity(mappedClaims));
        }

        return Task.FromResult(principal);
    }
}
