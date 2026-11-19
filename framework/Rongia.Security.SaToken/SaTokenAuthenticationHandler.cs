using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rongia.Security;

namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态认证处理器: 解析令牌 → 校验并续期 → 还原 ClaimsPrincipal
/// </summary>
public sealed class SaTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// 认证方案名
    /// </summary>
    public const string SchemeName = "SaToken";

    private readonly ISaTokenManager _tokenManager;
    private readonly SaTokenOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">认证方案选项</param>
    /// <param name="logger">日志工厂</param>
    /// <param name="encoder">URL 编码器</param>
    /// <param name="tokenManager">登录态管理</param>
    /// <param name="saTokenOptions">登录态配置</param>
    public SaTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISaTokenManager tokenManager,
        IOptions<SaTokenOptions> saTokenOptions)
        : base(options, logger, encoder)
    {
        ArgumentNullException.ThrowIfNull(tokenManager);
        ArgumentNullException.ThrowIfNull(saTokenOptions);
        _tokenManager = tokenManager;
        _options = saTokenOptions.Value;
    }

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = SaTokenResolver.ResolveToken(Context, _options);
        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        SaSession? session = await _tokenManager.GetSessionAsync(token, Context.RequestAborted);
        if (session is null)
        {
            return AuthenticateResult.Fail("登录态不存在或已过期。");
        }

        List<Claim> claims = [.. session.Claims.Select(claim => new Claim(claim.Type, claim.Value))];
        // 指定名称与角色声明类型, 使 [Authorize(Roles = "admin")] 与 User.Identity.Name 直接可用
        ClaimsIdentity identity = new(claims, SchemeName, UserClaimTypes.UserName, UserClaimTypes.Role);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
