using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Rongia.Security;

namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态管理默认实现: 令牌不携带业务信息, 登录态内容按令牌存在存储里
/// </summary>
public sealed class SaTokenManager : ISaTokenManager
{
    private const string RandomAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly ISaTokenStore _store;
    private readonly SaTokenOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="store">登录态存储</param>
    /// <param name="options">登录态配置</param>
    public SaTokenManager(ISaTokenStore store, IOptions<SaTokenOptions> options)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        _store = store;
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<string> LoginAsync(long userId, IEnumerable<KeyValuePair<string, string>>? claims = null, CancellationToken cancellationToken = default)
    {
        string userName = userId.ToString(CultureInfo.InvariantCulture);
        string name = userName;

        if (claims is not null)
        {
            List<KeyValuePair<string, string>> list = [.. claims];
            userName = FindValue(list, UserClaimTypes.UserName) ?? userName;
            name = FindValue(list, UserClaimTypes.Name) ?? userName;
        }

        return LoginAsync(userId, userName, name, claims, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> LoginAsync(long userId, string userName, string name, IEnumerable<KeyValuePair<string, string>>? claims = null, CancellationToken cancellationToken = default)
    {
        if (await IsDisabledAsync(userId, cancellationToken))
        {
            throw new SaDisableException(userId, await GetDisableRemainingAsync(userId, cancellationToken) ?? TimeSpan.Zero);
        }

        string loginId = userId.ToString(CultureInfo.InvariantCulture);

        if (!_options.IsConcurrent)
        {
            await _store.DeleteByLoginIdAsync(loginId, cancellationToken);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // 三条标准声明固定前置, 与 ICurrentUser 的读取口径一致
        List<SaClaim> sessionClaims =
        [
            new SaClaim { Type = UserClaimTypes.UserId, Value = loginId },
            new SaClaim { Type = UserClaimTypes.UserName, Value = userName },
            new SaClaim { Type = UserClaimTypes.Name, Value = name },
        ];

        if (claims is not null)
        {
            sessionClaims.AddRange(claims.Select(claim => new SaClaim { Type = claim.Key, Value = claim.Value }));
        }

        SaSession session = new()
        {
            Token = GenerateToken(),
            LoginId = loginId,
            Claims = sessionClaims,
            LoginTime = now,
            LastActiveTime = now,
            ExpiresAt = now.Add(_options.Timeout),
        };

        await _store.SaveAsync(session, cancellationToken);
        return session.Token;
    }

    /// <inheritdoc />
    public Task LogoutAsync(string token, CancellationToken cancellationToken = default) => _store.DeleteAsync(token, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IsLoginAsync(string token, CancellationToken cancellationToken = default)
        => await _store.FindAsync(token, cancellationToken) is not null;

    /// <inheritdoc />
    public async Task<long?> GetLoginIdAsync(string token, CancellationToken cancellationToken = default)
    {
        SaSession? session = await _store.FindAsync(token, cancellationToken);
        return session is null ? null : ParseLoginId(session.LoginId);
    }

    /// <inheritdoc />
    public async Task<SaSession?> GetSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        SaSession? session = await _store.FindAsync(token, cancellationToken);
        if (session is null)
        {
            return null;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool idleExpired = _options.ActiveTimeout > TimeSpan.Zero && now - session.LastActiveTime > _options.ActiveTimeout;
        if (session.ExpiresAt <= now || idleExpired)
        {
            await _store.DeleteAsync(token, cancellationToken);
            return null;
        }

        // 滑动续期(对齐 Java 版 Sa-Token: "状态存 Redis, 每次校验自动续期"):
        // 有效期内每访问一次, 有效期就从此刻重新起算 Timeout, 而不是登录那一刻起算的绝对过期
        session.LastActiveTime = now;
        session.ExpiresAt = now.Add(_options.Timeout);
        await _store.SaveAsync(session, cancellationToken);
        return session;
    }

    /// <inheritdoc />
    public async Task<SaTokenInfo?> GetTokenInfoAsync(string token, CancellationToken cancellationToken = default)
    {
        SaSession? session = await GetSessionAsync(token, cancellationToken);
        if (session is null)
        {
            return null;
        }

        return new SaTokenInfo
        {
            TokenName = _options.TokenName,
            TokenValue = session.Token,
            LoginId = ParseLoginId(session.LoginId) ?? 0,
            LoginTime = session.LoginTime,
            ExpiresAt = session.ExpiresAt,
        };
    }

    /// <inheritdoc />
    public async Task<int> KickoutAsync(long userId, CancellationToken cancellationToken = default)
    {
        string loginId = userId.ToString(CultureInfo.InvariantCulture);
        IReadOnlyList<SaSession> sessions = await _store.FindByLoginIdAsync(loginId, cancellationToken);
        await _store.DeleteByLoginIdAsync(loginId, cancellationToken);
        return sessions.Count;
    }

    /// <inheritdoc />
    public async Task<int> GetOnlineCountAsync(long userId, CancellationToken cancellationToken = default)
        => (await _store.FindByLoginIdAsync(userId.ToString(CultureInfo.InvariantCulture), cancellationToken)).Count;

    /// <inheritdoc />
    public async Task<int> DisableAsync(long userId, TimeSpan? duration = null, CancellationToken cancellationToken = default)
    {
        string loginId = userId.ToString(CultureInfo.InvariantCulture);
        DateTimeOffset until = duration is null ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.Add(duration.Value);
        await _store.SetDisableAsync(loginId, until, cancellationToken);

        IReadOnlyList<SaSession> sessions = await _store.FindByLoginIdAsync(loginId, cancellationToken);
        await _store.DeleteByLoginIdAsync(loginId, cancellationToken);
        return sessions.Count;
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
        DateTimeOffset? until = await _store.FindDisableAsync(userId.ToString(CultureInfo.InvariantCulture), cancellationToken);
        if (until is null)
        {
            return null;
        }

        if (until.Value == DateTimeOffset.MaxValue)
        {
            return TimeSpan.MaxValue;
        }

        TimeSpan remaining = until.Value - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            await _store.SetDisableAsync(userId.ToString(CultureInfo.InvariantCulture), null, cancellationToken);
            return null;
        }

        return remaining;
    }

    private static string? FindValue(List<KeyValuePair<string, string>> claims, string type)
        => claims.Where(claim => string.Equals(claim.Key, type, StringComparison.Ordinal)).Select(claim => claim.Value).FirstOrDefault();

    private static long? ParseLoginId(string loginId)
        => long.TryParse(loginId, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value : null;

    private string GenerateToken() => _options.TokenStyle switch
    {
        SaTokenStyle.Uuid => Guid.NewGuid().ToString(),
        SaTokenStyle.SimpleUuid => Guid.NewGuid().ToString("N"),
        SaTokenStyle.Random32 => RandomString(32),
        SaTokenStyle.Random64 => RandomString(64),
        _ => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
    };

    private static string RandomString(int length) => RandomNumberGenerator.GetString(RandomAlphabet, length);
}
