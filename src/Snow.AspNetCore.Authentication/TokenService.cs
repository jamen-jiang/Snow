using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Snow.DependencyInjection;
using Snow.Security.Claims;
using Snow.Timing;

namespace Snow.AspNetCore.Authentication;

/// <summary>
/// 令牌服务默认实现: 签发标准 JWT, 校验签名、签发者、受众, 再查服务端会话兜住登出、踢人与封禁
/// <para>白名单模型: 令牌有效当且仅当服务端仍存在对应会话, 所以登出、踢人、封禁只要删会话就立刻生效。</para>
/// <para>配置走 <see cref="IOptionsMonitor{TOptions}"/>: 改了 <c>Authentication</c> 节会重建签名密钥与校验参数, 不需要重启。</para>
/// </summary>
[SingletonService]
public sealed class TokenService : ITokenService
{
    private readonly ITokenSessionStore _store;
    private readonly IOptionsMonitor<SnowAuthenticationOptions> _optionsMonitor;
    private readonly IClock _clock;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly JsonWebTokenHandler _handler = new();

    // 配置快照: 选项实例变了才重建密钥与校验参数。单引用赋值, 并发重建最多浪费一次
    private Settings _settings;

    // 客户端字段上限: IP 与 UA 都来自请求, 不截断会让每条会话记录都可能很大
    private const int MaxClientFieldLength = 512;

    // 标准声明由签发逻辑自己写: 调用方再传一份会得到重复键, 读取时取到哪个取决于库内部顺序
    private static readonly HashSet<string> ReservedClaimTypes = new(StringComparer.Ordinal)
    {
        JwtRegisteredClaimNames.Jti,
        JwtRegisteredClaimNames.Exp,
        JwtRegisteredClaimNames.Iat,
        JwtRegisteredClaimNames.Nbf,
        JwtRegisteredClaimNames.Iss,
        JwtRegisteredClaimNames.Aud,
        SnowClaimTypes.UserId,
        SnowClaimTypes.UserName,
        SnowClaimTypes.Name,
    };

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="store">会话存储(在线登记与账号封禁)</param>
    /// <param name="options">JWT 配置(可热更新)</param>
    /// <param name="clock">时间提供器</param>
    /// <param name="httpContextAccessor">当前请求(用于自动记录登录来源, 非 Web 宿主可为 null)</param>
    public TokenService(
        ITokenSessionStore store,
        IOptionsMonitor<SnowAuthenticationOptions> options,
        IClock clock,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);

        _store = store;
        _optionsMonitor = options;
        _clock = clock;
        _httpContextAccessor = httpContextAccessor;
        _settings = BuildSettings(options.CurrentValue);
    }

    /// <inheritdoc />
    public async Task<string> LoginAsync(long userId, string userName, string name, IEnumerable<KeyValuePair<string, string>>? claims = null, TokenClientInfo? clientInfo = null, CancellationToken cancellationToken = default)
    {
        Settings settings = Current();
        SnowAuthenticationOptions options = settings.Options;
        string loginId = userId.ToString(CultureInfo.InvariantCulture);

        DateTimeOffset? disabledUntil = await GetDisableUntilAsync(loginId, cancellationToken);
        if (disabledUntil is not null)
        {
            throw new TokenDisabledException(userId, ToRemaining(disabledUntil.Value));
        }

        DateTimeOffset now = _clock.OffsetUtcNow;
        DateTimeOffset expiresAt = now.Add(options.Expiration);
        string jti = Guid.NewGuid().ToString("N");

        // 三条标准声明固定前置, 与 SnowClaimTypes 的读取口径一致; jti 用于把令牌对回会话
        List<Claim> tokenClaims =
        [
            new Claim(SnowClaimTypes.UserId, loginId),
            new Claim(SnowClaimTypes.UserName, userName),
            new Claim(SnowClaimTypes.Name, name),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
        ];

        if (claims is not null)
        {
            foreach (KeyValuePair<string, string> claim in claims)
            {
                if (ReservedClaimTypes.Contains(claim.Key))
                {
                    continue;
                }

                tokenClaims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        string token = CreateToken(settings, tokenClaims, now, expiresAt);
        TokenClientInfo? origin = clientInfo ?? CaptureClientInfo();

        // 会话是令牌有效的前提: 写失败即视为登录失败, 默认实现会抛, 交给宿主决定是否吞掉
        await _store.SaveAsync(
            new TokenSession
            {
                Jti = jti,
                LoginId = loginId,
                LoginTime = now,
                ExpiresAt = expiresAt,
                IpAddress = origin?.IpAddress,
                UserAgent = origin?.UserAgent,
            },
            cancellationToken);

        return token;
    }

    /// <inheritdoc />
    public async Task<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken cancellationToken = default)
    {
        CheckResult result = await CheckAsync(token, loadSession: false, cancellationToken);
        return result.Identity is null ? null : new ClaimsPrincipal(result.Identity);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        // 登出要幂等: 已登出的令牌再解一次仍然要拿到 jti, 所以这里不查会话
        if (await DecodeAsync(token, Current(), cancellationToken) is not DecodedToken decoded)
        {
            return;
        }

        await _store.DeleteAsync(decoded.Jti, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return false;
        }

        // DeleteAsync 会返回被删掉的会话, 不必先 Find 再删(那是两次缓存读)
        return await _store.DeleteAsync(jti, cancellationToken) is not null;
    }

    /// <inheritdoc />
    public async Task<TokenSession?> GetSessionAsync(string token, CancellationToken cancellationToken = default)
        => (await CheckAsync(token, loadSession: true, cancellationToken)).Session;

    /// <inheritdoc />
    public Task<IReadOnlyList<TokenSession>> GetSessionsAsync(long userId, CancellationToken cancellationToken = default)
        => _store.FindByLoginIdAsync(userId.ToString(CultureInfo.InvariantCulture), cancellationToken);

    /// <inheritdoc />
    public async Task<int> GetOnlineCountAsync(long userId, CancellationToken cancellationToken = default)
        => (await _store.FindByLoginIdAsync(userId.ToString(CultureInfo.InvariantCulture), cancellationToken)).Count;

    /// <inheritdoc />
    public Task<int> KickoutAsync(long userId, CancellationToken cancellationToken = default)
        => _store.DeleteByLoginIdAsync(userId.ToString(CultureInfo.InvariantCulture), cancellationToken);

    /// <inheritdoc />
    public async Task<int> DisableAsync(long userId, TimeSpan? duration = null, CancellationToken cancellationToken = default)
    {
        string loginId = userId.ToString(CultureInfo.InvariantCulture);
        DateTimeOffset until = duration is null ? DateTimeOffset.MaxValue : _clock.OffsetUtcNow.Add(duration.Value);
        await _store.SetDisableAsync(loginId, until, cancellationToken);

        // 封禁同时删掉现有会话: 只写封禁记录只能拦住下一次登录
        return await _store.DeleteByLoginIdAsync(loginId, cancellationToken);
    }

    /// <inheritdoc />
    public Task EnableAsync(long userId, CancellationToken cancellationToken = default)
        => _store.SetDisableAsync(userId.ToString(CultureInfo.InvariantCulture), null, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IsDisabledAsync(long userId, CancellationToken cancellationToken = default)
        => await GetDisableRemainingAsync(userId, cancellationToken) is not null;

    /// <inheritdoc />
    public async Task<TimeSpan?> GetDisableRemainingAsync(long userId, CancellationToken cancellationToken = default)
    {
        string loginId = userId.ToString(CultureInfo.InvariantCulture);
        DateTimeOffset? until = await GetDisableUntilAsync(loginId, cancellationToken);
        if (until is null)
        {
            return null;
        }

        TimeSpan remaining = ToRemaining(until.Value);
        if (remaining <= TimeSpan.Zero)
        {
            await _store.SetDisableAsync(loginId, null, cancellationToken);
            return null;
        }

        return remaining;
    }

    /// <summary>
    /// 续期会话并确认它仍然有效: 验签交给框架的 JwtBearer 处理器之后, 由模块调用补上白名单这一道
    /// <para>返回 null 表示会话已被删(登出/踢人/封禁)或已到期, 令牌应当作废。</para>
    /// </summary>
    /// <param name="jti">令牌的 <c>jti</c>, 用来找回会话</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<TokenSession?> RenewSessionAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(jti))
        {
            return null;
        }

        TokenSession? session = await _store.FindAsync(jti, cancellationToken);
        if (session is null)
        {
            return null;
        }

        DateTimeOffset now = _clock.OffsetUtcNow;
        if (session.ExpiresAt <= now)
        {
            return null;
        }

        Renew(session, now, Current().Options);
        await _store.TouchAsync(session, cancellationToken);

        return session;
    }

    /// <summary>
    /// 续期会话: 每次访问都把到期时间往后推一个窗口
    /// <para>直接改传进来的 <paramref name="session"/>, 由调用方决定什么时候写回缓存。</para>
    /// </summary>
    private static void Renew(TokenSession session, DateTimeOffset now, SnowAuthenticationOptions options)
        => session.ExpiresAt = now + options.Expiration;

    /// <summary>
    /// 校验令牌并读会话
    /// <para>白名单 + 滑动续期: 会话不存在或已到期一律当未登录, 所以 <paramref name="loadSession"/>
    /// 只决定结果里要不要带回会话对象, 不影响判定。</para>
    /// </summary>
    private async Task<CheckResult> CheckAsync(string token, bool loadSession, CancellationToken cancellationToken)
    {
        Settings settings = Current();
        if (await DecodeAsync(token, settings, cancellationToken) is not DecodedToken decoded)
        {
            return default;
        }

        // 时间全由 IClock 决定: 校验参数里关掉了 ValidateLifetime, 库不再按系统时间判 exp/nbf。
        // 令牌里的 exp 只是签发时的快照, 真正判过期的是会话的到期时间(滑动续期会把它推到 exp 之后)
        DateTimeOffset now = _clock.OffsetUtcNow;
        if (decoded.NotBefore is DateTimeOffset notBefore && notBefore > now)
        {
            return default;
        }

        TokenSession? session = await _store.FindAsync(decoded.Jti, cancellationToken);
        if (session is null)
        {
            // 会话被删(登出/踢人/封禁)或缓存被清, 令牌即失效
            return default;
        }

        if (session.ExpiresAt <= now)
        {
            // 停用超过一个窗口: 会话自己到期, 缓存键也会跟着过期, 不用额外清理
            return default;
        }

        Renew(session, now, settings.Options);
        await _store.TouchAsync(session, cancellationToken);

        return new CheckResult(decoded.Identity, loadSession ? session : null, decoded.Jti, decoded.IssuedAt, session.ExpiresAt);
    }

    /// <summary>
    /// 只做密码学校验并解出 jti 与时间, 不看会话
    /// <para>没有 <c>exp</c> 的令牌直接拒绝: 签发时一定写了, 缺了只能是伪造或裁剪过的。</para>
    /// </summary>
    private async Task<DecodedToken?> DecodeAsync(string token, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        TokenValidationResult result;
        try
        {
            result = await _handler.ValidateTokenAsync(token, settings.ValidationParameters);
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            // 结构损坏、算法不符、签名不对都落到这里, 一律当作未登录
            return null;
        }

        if (!result.IsValid || result.ClaimsIdentity is null)
        {
            return null;
        }

        ClaimsIdentity identity = result.ClaimsIdentity;
        string? jti = identity.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(jti))
        {
            return null;
        }

        DateTimeOffset? expiresAt = ReadTime(identity, JwtRegisteredClaimNames.Exp);
        if (expiresAt is null)
        {
            return null;
        }

        return new DecodedToken(
            identity,
            jti,
            ReadTime(identity, JwtRegisteredClaimNames.Iat) ?? _clock.OffsetUtcNow,
            expiresAt.Value,
            ReadTime(identity, JwtRegisteredClaimNames.Nbf));
    }

    /// <summary>
    /// 取当前配置快照, 配置变了就重建
    /// </summary>
    private Settings Current()
    {
        SnowAuthenticationOptions options = _optionsMonitor.CurrentValue;
        Settings cached = _settings;
        if (ReferenceEquals(cached.Options, options))
        {
            return cached;
        }

        Settings fresh = BuildSettings(options);
        _settings = fresh;
        return fresh;
    }

    private static Settings BuildSettings(SnowAuthenticationOptions options)
    {
        SymmetricSecurityKey key = TokenKeys.CreateKey(options);
        SigningCredentials credentials = new(key, TokenKeys.Algorithm);
        TokenValidationParameters validationParameters = TokenKeys.CreateValidationParameters(options, key);
        return new Settings(options, credentials, validationParameters);
    }

    /// <summary>
    /// 按给定声明与时间签一份令牌: iss/aud/算法口径集中在这里, 登录是唯一调用方
    /// </summary>
    private string CreateToken(Settings settings, List<Claim> claims, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        SecurityTokenDescriptor descriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            // 与校验口径一致(GetIssuer 去尾部斜杠), 签发与校验用同一个 iss
            Issuer = settings.Options.GetIssuer(),
            Audience = settings.Options.GetAudience(),
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = settings.Credentials,
        };

        return _handler.CreateToken(descriptor);
    }

    /// <summary>
    /// 取当前请求的来源信息: 调用方没显式给 <see cref="TokenClientInfo"/> 时用它填在线登记
    /// </summary>
    private TokenClientInfo? CaptureClientInfo()
    {
        HttpContext? context = _httpContextAccessor?.HttpContext;
        if (context is null)
        {
            return null;
        }

        string? ipAddress = ResolveIpAddress(context);
        string? userAgent = Truncate(context.Request.Headers.UserAgent.ToString());
        if (ipAddress is null && userAgent is null)
        {
            return null;
        }

        return new TokenClientInfo { IpAddress = ipAddress, UserAgent = userAgent };
    }

    /// <summary>
    /// 查封禁截止时间, 已到期顺手清掉
    /// </summary>
    private async Task<DateTimeOffset?> GetDisableUntilAsync(string loginId, CancellationToken cancellationToken)
    {
        DateTimeOffset? until = await _store.FindDisableAsync(loginId, cancellationToken);
        if (until is null)
        {
            return null;
        }

        if (until.Value != DateTimeOffset.MaxValue && until.Value <= _clock.OffsetUtcNow)
        {
            await _store.SetDisableAsync(loginId, null, cancellationToken);
            return null;
        }

        return until;
    }

    private TimeSpan ToRemaining(DateTimeOffset until)
        => until == DateTimeOffset.MaxValue ? TimeSpan.MaxValue : until - _clock.OffsetUtcNow;

    /// <summary>
    /// 读 JWT 里的秒级时间戳声明(iat / exp), 缺失或格式不对返回 null
    /// </summary>
    private static DateTimeOffset? ReadTime(ClaimsIdentity identity, string claimType)
    {
        string? value = identity.FindFirst(claimType)?.Value;
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }

    /// <summary>
    /// 取客户端 IP: 反向代理之后 <c>RemoteIpAddress</c> 是代理地址, 优先用 <c>X-Forwarded-For</c> 的第一段
    /// <para>该头可被客户端伪造, 只用于展示设备来源, 不参与任何校验。</para>
    /// </summary>
    private static string? ResolveIpAddress(HttpContext context)
    {
        string forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (forwarded.Length > 0)
        {
            int separator = forwarded.IndexOf(',', StringComparison.Ordinal);
            string first = (separator < 0 ? forwarded : forwarded[..separator]).Trim();
            if (first.Length > 0)
            {
                return Truncate(first);
            }
        }

        return Truncate(context.Connection.RemoteIpAddress?.ToString());
    }

    private static string? Truncate(string? value)
        => string.IsNullOrEmpty(value) ? null : value.Length <= MaxClientFieldLength ? value : value[..MaxClientFieldLength];

    /// <summary>
    /// 校验通过的令牌: 主体 + jti + 时间
    /// </summary>
    private readonly record struct DecodedToken(ClaimsIdentity Identity, string Jti, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, DateTimeOffset? NotBefore);

    /// <summary>
    /// 校验结果: 未通过时 <see cref="Identity"/> 为 null
    /// </summary>
    private readonly record struct CheckResult(ClaimsIdentity? Identity, TokenSession? Session, string? Jti, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);

    /// <summary>
    /// 配置快照: 一个选项实例对应一套密钥与一套校验参数
    /// </summary>
    private sealed record Settings(SnowAuthenticationOptions Options, SigningCredentials Credentials, TokenValidationParameters ValidationParameters);
}
