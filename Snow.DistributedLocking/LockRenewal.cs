using Microsoft.Extensions.Logging;

namespace Snow.DistributedLocking;

/// <summary>
/// 一次持锁对应的续期循环(对应 Redisson 的 <c>ExpirationEntry</c>): 每「租期三分之一」续期一次, 续不上即判定丢锁。
/// <para>一条循环只服务一个锁句柄; 可重入的锁在重入时共享句柄, 因此同一把锁同一时刻也只有这一条循环,
/// 不存在 Redisson 里「按锁名去重」导致的续错对象问题。</para>
/// <para>每次触发先看 <see cref="IDistributedLockHandle.HandleLostToken"/>, 再调
/// <see cref="IDistributedLockHandle.TryExtendAsync"/>; 任一步失败即认定锁已丢失(Redisson 的
/// <c>cancelExpirationRenewal</c> 语义), 取消 <see cref="LostToken"/> 并记录错误。</para>
/// </summary>
public sealed class LockRenewal : IAsyncDisposable
{
    private readonly CancellationTokenSource _stopSource = new();
    private readonly CancellationTokenSource _lostSource = new();
    private readonly IDistributedLockHandle _handle;
    private readonly TimeSpan _leaseTime;
    private readonly TimeSpan _interval;
    private readonly ILogger? _logger;
    private readonly string _key;
    private Task? _loop;
    private int _started;
    private int _disposed;

    internal LockRenewal(
        IDistributedLockHandle handle,
        string key,
        TimeSpan leaseTime,
        TimeSpan interval,
        ILogger? logger)
    {
        _handle = handle;
        _key = key;
        _leaseTime = leaseTime;
        _interval = interval;
        _logger = logger;
    }

    /// <summary>
    /// 续期对应的锁 key, 仅用于日志
    /// </summary>
    public string LockKey => _key;

    /// <summary>
    /// 锁丢失信号: 看门狗确认锁已不在手里时取消, 业务可链进自己的取消令牌
    /// </summary>
    public CancellationToken LostToken => _lostSource.Token;

    /// <summary>
    /// 锁是否已丢失(续期失败、连接断开或锁被他人持有)
    /// </summary>
    public bool IsLockLost => _lostSource.IsCancellationRequested;

    /// <summary>
    /// 启动续期循环; 重复调用只生效一次
    /// </summary>
    internal void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        if (_handle.HandleLostToken.IsCancellationRequested)
        {
            MarkLost("持锁时锁已丢失");
            return;
        }

        _loop = RunAsync();
    }

    /// <summary>
    /// 停止续期并等待循环退出
    /// </summary>
    /// <returns>异步任务</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            await _stopSource.CancelAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // 已停止
        }

        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 正常停止
            }
        }

        _stopSource.Dispose();
    }

    private async Task RunAsync()
    {
        while (!_stopSource.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, _stopSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_handle.HandleLostToken.IsCancellationRequested)
            {
                MarkLost("锁丢失信号已触发(续期失败或连接断开)");
                return;
            }

            bool extended;

            try
            {
                extended = await _handle.TryExtendAsync(_leaseTime, _stopSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                MarkLost("续期异常: " + exception.Message);
                return;
            }

            if (!extended)
            {
                MarkLost("续期返回失败");
                return;
            }
        }
    }

    /// <summary>
    /// 标记丢锁: 只触发一次
    /// </summary>
    /// <param name="reason">丢失原因, 写日志用</param>
    private void MarkLost(string reason)
    {
        if (_lostSource.IsCancellationRequested)
        {
            return;
        }

        _logger?.LogError("分布式锁已丢失: key 为 {LockKey}, 原因: {Reason}", _key, reason);

        try
        {
            _lostSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // 已释放
        }
    }
}
