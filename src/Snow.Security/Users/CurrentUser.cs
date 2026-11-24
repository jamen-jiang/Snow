using System.Security.Claims;
using System.Security.Principal;
using Snow.DependencyInjection;
using Snow.Security.Claims;

namespace Snow.Security.Users;

/// <summary>
/// 当前用户默认实现: 一切数据都从 <see cref="ICurrentPrincipalAccessor"/> 的当前主体读取
/// <para>对齐 ABP 的 <c>CurrentUser</c>。认证方案(不透明令牌 / 会话)只负责产出 ClaimsPrincipal, 这里与之解耦。</para>
/// </summary>
[TransientService]
public class CurrentUser : ICurrentUser
{
    private static readonly Claim[] EmptyClaimsArray = [];

    private readonly ICurrentPrincipalAccessor _principalAccessor;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="principalAccessor">当前主体访问器</param>
    public CurrentUser(ICurrentPrincipalAccessor principalAccessor)
    {
        ArgumentNullException.ThrowIfNull(principalAccessor);
        _principalAccessor = principalAccessor;
    }

    /// <inheritdoc />
    public virtual bool IsAuthenticated => Id.HasValue;

    /// <inheritdoc />
    public virtual long? Id => _principalAccessor.Principal?.FindUserId();

    /// <inheritdoc />
    public virtual string? UserName => this.FindClaimValue(SnowClaimTypes.UserName);

    /// <inheritdoc />
    public virtual string? Name => this.FindClaimValue(SnowClaimTypes.Name);

    /// <inheritdoc />
    public virtual Guid? TenantId
    {
        get
        {
            string? value = this.FindClaimValue(SnowClaimTypes.TenantId);
            return Guid.TryParse(value, out Guid tenantId) ? tenantId : null;
        }
    }

    /// <inheritdoc />
    public virtual string? PhoneNumber => this.FindClaimValue(SnowClaimTypes.MobilePhone);

    /// <inheritdoc />
    public virtual string? Email => this.FindClaimValue(SnowClaimTypes.Email);

    /// <inheritdoc />
    public virtual string[] Roles => FindClaims(SnowClaimTypes.Role).Select(claim => claim.Value).Distinct(StringComparer.Ordinal).ToArray();

    /// <inheritdoc />
    public virtual Claim? FindClaim(string claimType)
    {
        return _principalAccessor.Principal?.Claims.FirstOrDefault(claim => claim.Type == claimType);
    }

    /// <inheritdoc />
    public virtual Claim[] GetAllClaims()
    {
        return _principalAccessor.Principal?.Claims.ToArray() ?? EmptyClaimsArray;
    }

    /// <inheritdoc />
    public virtual bool IsInRole(string roleName)
    {
        return FindClaims(SnowClaimTypes.Role).Any(claim => claim.Value == roleName);
    }

    private Claim[] FindClaims(string claimType)
    {
        return _principalAccessor.Principal?.Claims.Where(claim => claim.Type == claimType).ToArray() ?? EmptyClaimsArray;
    }
}
