using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Snow.Timing;

namespace Snow.Authentication;

/// <summary>
/// 基于 <see cref="IDistributedCache"/> 的会话存储: 在线会话登记与账号封禁
/// <para>有效性模型是白名单: 校验时按 <c>jti</c> 读会话, 读不到就当作已下线。登出、踢人、封禁都是删会话。</para>
/// <para>宿主注册 Redis 即多实例共享, 踢人与封禁跨实例生效。</para>
/// <para>缓存里读到解析不了的字节一律当作"没有": 一条坏键不该让该账号的所有请求都 500。</para>
/// </summary>
public sealed class DistributedCacheTokenSessionStore : ITokenSessionStore
{
    private const string SessionKeyPrefix = "snow:jsonwebtoken:";
    private const string AccountIndexKeyPrefix = "snow:jsonwebtoken-account:";
    private const string DisableKeyPrefix = "snow:jsonwebtoken-disabled:";

    /// <summary>
    /// 永久封禁的写入时长(100 年): 显式给一个上限, 不依赖"空 DistributedCacheEntryOptions 表示永不过期"这一实现细节
    /// </summary>
    private static readonly TimeSpan PermanentDisableLifetime = TimeSpan.FromDays(36500);

    /// <summary>
    /// 账号索引的最短寿命: 索引只存 jti 列表, 给足冗余比精确到期更安全
    /// <para>缓存读不到剩余 TTL, 若按"本次会话的过期时间"写, 同账号先登录的长会话会被后登录的短会话把索引 TTL 压短,
    /// 索引先死就让踢人与在线数漏掉仍然有效的令牌。失效 jti 会在 <see cref="FindByLoginIdAsync"/> 里惰性剔除。</para>
    /// </summary>
    private static readonly TimeSpan IndexLifetime = TimeSpan.FromDays(30);

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly IClock _clock;

    // 账号索引是读-改-写, 并发登录不串行就会互相覆盖(丢 jti 表现为踢人漏设备)。
    // 这里按 loginId 哈希到固定数量的门, 内存有界; 跨进程仍无原子性(缓存没有 CAS), 属尽力而为。
    private readonly SemaphoreSlim[] _indexGates = Enumerable.Range(0, 32).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="cache">分布式缓存</param>
    /// <param name="clock">时间提供器</param>
    public DistributedCacheTokenSessionStore(IDistributedCache cache, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(clock);
        _cache = cache;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task SaveAsync(TokenSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await _cache.SetAsync(
            SessionKey(session.Jti),
            JsonSerializer.SerializeToUtf8Bytes(session, SerializerOptions),
            new DistributedCacheEntryOptions { AbsoluteExpiration = session.ExpiresAt },
            cancellationToken);

        // 索引 TTL 统一走 IndexExpiresAt: 用 session.ExpiresAt 会让先登出的短会话把索引 TTL 压到比晚会话还短
        await MutateIndexAsync(
            session.LoginId,
            IndexExpiresAt(session.ExpiresAt),
            tokens => tokens.Contains(session.Jti, StringComparer.Ordinal) ? null : [.. tokens, session.Jti],
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task TouchAsync(TokenSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        // 会话键重写一次, AbsoluteExpiration 跟着新的到期时间走
        await _cache.SetAsync(
            SessionKey(session.Jti),
            JsonSerializer.SerializeToUtf8Bytes(session, SerializerOptions),
            new DistributedCacheEntryOptions { AbsoluteExpiration = session.ExpiresAt },
            cancellationToken);

        // 索引寿命跟着续: 长期在线的账号若只续会话不续索引, 索引会先过期, 踢人与在线数就漏掉这台设备
        // 即使 jti 已在索引里也要写回 —— 写回的目的是刷新索引 TTL, 返回 null 会把 TTL 停在旧值上
        await MutateIndexAsync(
            session.LoginId,
            IndexExpiresAt(session.ExpiresAt),
            tokens => tokens.Contains(session.Jti, StringComparer.Ordinal) ? tokens : [.. tokens, session.Jti],
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TokenSession?> FindAsync(string jti, CancellationToken cancellationToken = default)
    {
        byte[]? payload = await _cache.GetAsync(SessionKey(jti), cancellationToken);
        return TryRead<TokenSession>(payload, out TokenSession? session) ? session : null;
    }

    /// <inheritdoc />
    public async Task<TokenSession?> DeleteAsync(string jti, CancellationToken cancellationToken = default)
    {
        // 先读一次拿 loginId: 会话删掉之后就不知道它属于哪个账号了。返回值顺带让调用方不必再读一遍
        TokenSession? session = await FindAsync(jti, cancellationToken);
        await _cache.RemoveAsync(SessionKey(jti), cancellationToken);

        if (session is null)
        {
            return null;
        }

        await MutateIndexAsync(
            session.LoginId,
            IndexExpiresAt(session.ExpiresAt),
            tokens => tokens.Contains(jti, StringComparer.Ordinal)
                ? tokens.Where(item => !string.Equals(item, jti, StringComparison.Ordinal)).ToList()
                : null,
            cancellationToken);

        return session;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TokenSession>> FindByLoginIdAsync(string loginId, CancellationToken cancellationToken = default)
    {
        List<string> tokens = await GetAccountTokensAsync(loginId, cancellationToken);
        if (tokens.Count == 0)
        {
            return [];
        }

        // 并发读: 多端登录时串行读是 N 次往返
        TokenSession?[] found = await Task.WhenAll(tokens.Select(jti => FindAsync(jti, cancellationToken)));

        List<TokenSession> sessions = new(found.Length);
        HashSet<string> alive = new(StringComparer.Ordinal);
        DateTimeOffset indexExpiresAt = _clock.OffsetUtcNow.AddDays(1);
        for (int index = 0; index < found.Length; index++)
        {
            if (found[index] is not TokenSession session)
            {
                continue;
            }

            sessions.Add(session);
            alive.Add(tokens[index]);

            // 索引 TTL 不能短于最晚过期的会话, 否则索引先过期会让踢人与在线数找不到仍然有效的令牌
            if (session.ExpiresAt > indexExpiresAt)
            {
                indexExpiresAt = session.ExpiresAt;
            }
        }

        if (alive.Count != tokens.Count)
        {
            // 在门里重读一次再改: 直接用上面读到的旧列表写回会盖掉并发登录新加的 jti。
            // 过滤条件也必须以本次快照为界 —— 只摘掉"这次读到且确认已死"的 jti;
            // 重读时才出现的新 jti 不在快照里, 用 alive 过滤会把它一起摘掉(索引里从此看不到那台设备)。
            await MutateIndexAsync(
                loginId,
                IndexExpiresAt(indexExpiresAt),
                current =>
                {
                    List<string> next = current
                        .Where(jti => !tokens.Contains(jti, StringComparer.Ordinal) || alive.Contains(jti))
                        .ToList();
                    return next.Count == current.Count ? null : next;
                },
                cancellationToken);
        }

        return sessions;
    }

    /// <inheritdoc />
    public async Task<int> DeleteByLoginIdAsync(string loginId, CancellationToken cancellationToken = default)
    {
        List<string> tokens = await GetAccountTokensAsync(loginId, cancellationToken);
        if (tokens.Count == 0)
        {
            return 0;
        }

        TokenSession?[] found = await Task.WhenAll(tokens.Select(jti => FindAsync(jti, cancellationToken)));
        await Task.WhenAll(tokens.Select(jti => _cache.RemoveAsync(SessionKey(jti), cancellationToken)));

        // 只摘掉本次删掉的 jti: 无条件清空会连同并发登录刚写进去的索引一起删, 那些会话之后在踢人与在线数里永远不可见
        // 索引 TTL 也不能写成"现在": 那等于把索引立刻作废, 同一效果。剩下的 jti 属于并发登录的会话,
        // 这里拿不到它们的到期时间, 用 IndexExpiresAt 的最短寿命兜底(失效项由 FindByLoginIdAsync 惰性剔除)。
        await MutateIndexAsync(
            loginId,
            IndexExpiresAt(_clock.OffsetUtcNow),
            current => current.Any(jti => tokens.Contains(jti, StringComparer.Ordinal))
                ? current.Where(jti => !tokens.Contains(jti, StringComparer.Ordinal)).ToList()
                : null,
            cancellationToken);

        return found.Count(session => session is not null);
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> FindDisableAsync(string loginId, CancellationToken cancellationToken = default)
    {
        byte[]? payload = await _cache.GetAsync(DisableKey(loginId), cancellationToken);
        return TryRead<DateTimeOffset>(payload, out DateTimeOffset until) ? until : null;
    }

    /// <inheritdoc />
    public Task SetDisableAsync(string loginId, DateTimeOffset? until, CancellationToken cancellationToken = default)
    {
        if (until is null)
        {
            return _cache.RemoveAsync(DisableKey(loginId), cancellationToken);
        }

        DistributedCacheEntryOptions entryOptions = until.Value == DateTimeOffset.MaxValue
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = PermanentDisableLifetime }
            : new DistributedCacheEntryOptions { AbsoluteExpiration = until.Value };

        return _cache.SetAsync(DisableKey(loginId), JsonSerializer.SerializeToUtf8Bytes(until.Value, SerializerOptions), entryOptions, cancellationToken);
    }

    /// <summary>
    /// 读-改-写账号索引: 整个过程在按 loginId 分到的那道门里完成, 因此本进程内并发安全
    /// </summary>
    /// <param name="loginId">登录Id</param>
    /// <param name="expiresAt">索引过期时间</param>
    /// <param name="mutate">拿到当前列表返回新列表; 返回 null 表示不用写</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task MutateIndexAsync(string loginId, DateTimeOffset expiresAt, Func<List<string>, List<string>?> mutate, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = IndexGate(loginId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            List<string> tokens = await GetAccountTokensAsync(loginId, cancellationToken);
            List<string>? next = mutate(tokens);
            if (next is null)
            {
                return;
            }

            if (next.Count == 0)
            {
                await _cache.RemoveAsync(AccountIndexKey(loginId), cancellationToken);
                return;
            }

            await WriteAccountTokensAsync(loginId, next, expiresAt, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<string>> GetAccountTokensAsync(string loginId, CancellationToken cancellationToken)
    {
        byte[]? payload = await _cache.GetAsync(AccountIndexKey(loginId), cancellationToken);
        return TryRead<List<string>>(payload, out List<string>? tokens) && tokens is not null ? tokens : [];
    }

    /// <summary>
    /// 索引至少要活到最后一个会话过期, 否则索引先过期会让踢人漏掉仍然有效的令牌
    /// </summary>
    private DateTimeOffset IndexExpiresAt(DateTimeOffset sessionExpiresAt)
    {
        DateTimeOffset floor = _clock.OffsetUtcNow.Add(IndexLifetime);
        return sessionExpiresAt > floor ? sessionExpiresAt : floor;
    }

    private Task WriteAccountTokensAsync(string loginId, List<string> tokens, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        => _cache.SetAsync(
            AccountIndexKey(loginId),
            JsonSerializer.SerializeToUtf8Bytes(tokens, SerializerOptions),
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt },
            cancellationToken);

    /// <summary>
    /// 反序列化兜底: 键被别的版本或别的程序写坏时当作没有, 不把异常抛到请求路径上
    /// </summary>
    private static bool TryRead<T>(byte[]? payload, out T? value)
    {
        value = default;
        if (payload is null || payload.Length == 0)
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(payload, SerializerOptions);
            return value is not null;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return false;
        }
    }

    private SemaphoreSlim IndexGate(string loginId)
        => _indexGates[(int)((uint)StringComparer.Ordinal.GetHashCode(loginId) % (uint)_indexGates.Length)];

    private static string SessionKey(string jti) => SessionKeyPrefix + jti;

    private static string AccountIndexKey(string loginId) => AccountIndexKeyPrefix + loginId;

    private static string DisableKey(string loginId) => DisableKeyPrefix + loginId;
}
