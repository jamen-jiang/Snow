using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Snow.Security.SaToken;

/// <summary>
/// 进程内登录态存储(默认实现): 零外部依赖, 单实例部署开箱可用
/// </summary>
public sealed class MemorySaTokenStore : ISaTokenStore
{
    private readonly ConcurrentDictionary<string, SaSession> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _disabled = new(StringComparer.Ordinal);
    private readonly SaTokenOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">登录态配置</param>
    public MemorySaTokenStore(IOptions<SaTokenOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<SaSession?> FindAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(token, out SaSession? session))
        {
            return Task.FromResult<SaSession?>(null);
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return Task.FromResult<SaSession?>(null);
        }

        return Task.FromResult<SaSession?>(session);
    }

    /// <inheritdoc />
    public Task SaveAsync(SaSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        _sessions[session.Token] = session;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string token, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(token, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SaSession>> FindByLoginIdAsync(string loginId, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<SaSession> result = [];
        foreach ((string token, SaSession session) in _sessions)
        {
            if (session.ExpiresAt <= now)
            {
                _sessions.TryRemove(token, out _);
                continue;
            }

            if (string.Equals(session.LoginId, loginId, StringComparison.Ordinal))
            {
                result.Add(session);
            }
        }

        return Task.FromResult<IReadOnlyList<SaSession>>(result);
    }

    /// <inheritdoc />
    public Task DeleteByLoginIdAsync(string loginId, CancellationToken cancellationToken = default)
    {
        foreach ((string token, SaSession session) in _sessions)
        {
            if (string.Equals(session.LoginId, loginId, StringComparison.Ordinal))
            {
                _sessions.TryRemove(token, out _);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DateTimeOffset?> FindDisableAsync(string loginId, CancellationToken cancellationToken = default)
    {
        if (!_disabled.TryGetValue(loginId, out DateTimeOffset until))
        {
            return Task.FromResult<DateTimeOffset?>(null);
        }

        if (until != DateTimeOffset.MaxValue && until <= DateTimeOffset.UtcNow)
        {
            _disabled.TryRemove(loginId, out _);
            return Task.FromResult<DateTimeOffset?>(null);
        }

        return Task.FromResult<DateTimeOffset?>(until);
    }

    /// <inheritdoc />
    public Task SetDisableAsync(string loginId, DateTimeOffset? until, CancellationToken cancellationToken = default)
    {
        if (until is null)
        {
            _disabled.TryRemove(loginId, out _);
        }
        else
        {
            _disabled[loginId] = until.Value;
        }

        return Task.CompletedTask;
    }
}
