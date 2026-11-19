using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Rongia.Security.SaToken;

/// <summary>
/// 基于 <see cref="IDistributedCache"/> 的登录态存储: 宿主注册 Redis 即多实例共享
/// </summary>
public sealed class DistributedCacheSaTokenStore : ISaTokenStore
{
    private const string SessionKeyPrefix = "rongia:satoken:";
    private const string AccountIndexKeyPrefix = "rongia:satoken-account:";
    private const string DisableKeyPrefix = "rongia:satoken-disabled:";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly SaTokenOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="cache">分布式缓存</param>
    /// <param name="options">登录态配置</param>
    public DistributedCacheSaTokenStore(IDistributedCache cache, IOptions<SaTokenOptions> options)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        _cache = cache;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<SaSession?> FindAsync(string token, CancellationToken cancellationToken = default)
    {
        byte[]? payload = await _cache.GetAsync(SessionKey(token), cancellationToken);
        return payload is null ? null : JsonSerializer.Deserialize<SaSession>(payload, SerializerOptions);
    }

    /// <inheritdoc />
    public async Task SaveAsync(SaSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        DistributedCacheEntryOptions entryOptions = new() { AbsoluteExpiration = session.ExpiresAt };
        if (_options.ActiveTimeout > TimeSpan.Zero)
        {
            entryOptions.SlidingExpiration = _options.ActiveTimeout;
        }

        await _cache.SetAsync(SessionKey(session.Token), JsonSerializer.SerializeToUtf8Bytes(session, SerializerOptions), entryOptions, cancellationToken);

        // 账号->令牌索引的 TTL 跟着会话一起滑动, 否则索引先过期会导致 Kickout 找不到令牌
        List<string> tokens = await GetAccountTokensAsync(session.LoginId, cancellationToken);
        if (!tokens.Contains(session.Token, StringComparer.Ordinal))
        {
            tokens.Add(session.Token);
        }

        await WriteAccountTokensAsync(session.LoginId, tokens, session.ExpiresAt, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string token, CancellationToken cancellationToken = default)
    {
        SaSession? session = await FindAsync(token, cancellationToken);
        await _cache.RemoveAsync(SessionKey(token), cancellationToken);

        if (session is not null)
        {
            List<string> tokens = await GetAccountTokensAsync(session.LoginId, cancellationToken);
            if (tokens.RemoveAll(item => string.Equals(item, token, StringComparison.Ordinal)) > 0)
            {
                await WriteAccountTokensAsync(session.LoginId, tokens, session.ExpiresAt, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SaSession>> FindByLoginIdAsync(string loginId, CancellationToken cancellationToken = default)
    {
        List<string> tokens = await GetAccountTokensAsync(loginId, cancellationToken);
        List<SaSession> sessions = new(tokens.Count);
        List<string> aliveTokens = new(tokens.Count);

        foreach (string token in tokens)
        {
            SaSession? session = await FindAsync(token, cancellationToken);
            if (session is not null)
            {
                sessions.Add(session);
                aliveTokens.Add(token);
            }
        }

        if (aliveTokens.Count != tokens.Count)
        {
            await WriteAccountTokensAsync(loginId, aliveTokens, DateTimeOffset.UtcNow.Add(_options.Timeout), cancellationToken);
        }

        return sessions;
    }

    /// <inheritdoc />
    public async Task DeleteByLoginIdAsync(string loginId, CancellationToken cancellationToken = default)
    {
        List<string> tokens = await GetAccountTokensAsync(loginId, cancellationToken);
        foreach (string token in tokens)
        {
            await _cache.RemoveAsync(SessionKey(token), cancellationToken);
        }

        await _cache.RemoveAsync(AccountIndexKey(loginId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> FindDisableAsync(string loginId, CancellationToken cancellationToken = default)
    {
        byte[]? payload = await _cache.GetAsync(DisableKey(loginId), cancellationToken);
        return payload is null ? null : JsonSerializer.Deserialize<DateTimeOffset>(payload, SerializerOptions);
    }

    /// <inheritdoc />
    public Task SetDisableAsync(string loginId, DateTimeOffset? until, CancellationToken cancellationToken = default)
    {
        if (until is null)
        {
            return _cache.RemoveAsync(DisableKey(loginId), cancellationToken);
        }

        DistributedCacheEntryOptions entryOptions = until.Value == DateTimeOffset.MaxValue
            ? new DistributedCacheEntryOptions()
            : new DistributedCacheEntryOptions { AbsoluteExpiration = until.Value };

        return _cache.SetAsync(DisableKey(loginId), JsonSerializer.SerializeToUtf8Bytes(until.Value, SerializerOptions), entryOptions, cancellationToken);
    }

    private async Task<List<string>> GetAccountTokensAsync(string loginId, CancellationToken cancellationToken)
    {
        byte[]? payload = await _cache.GetAsync(AccountIndexKey(loginId), cancellationToken);
        return payload is null ? [] : JsonSerializer.Deserialize<List<string>>(payload, SerializerOptions) ?? [];
    }

    private Task WriteAccountTokensAsync(string loginId, List<string> tokens, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        => _cache.SetAsync(
            AccountIndexKey(loginId),
            JsonSerializer.SerializeToUtf8Bytes(tokens, SerializerOptions),
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt },
            cancellationToken);

    private static string SessionKey(string token) => SessionKeyPrefix + token;

    private static string AccountIndexKey(string loginId) => AccountIndexKeyPrefix + loginId;

    private static string DisableKey(string loginId) => DisableKeyPrefix + loginId;
}
