using StackExchange.Redis;

namespace Snow.DistributedLocking;

/// <summary>
/// 一次 Redis 抢锁成功对应的内部状态(对应 Redisson 的 <c>RedisLockEntry</c>): 键名、token、持有栈与续期句柄。
/// <para>可重入的每次重入只往持有栈里压一个调用链节点, 共享同一个 entry; 逐层摘除, 栈空了才停续期并删键,
/// 因此同一把锁同一时刻只有一条续期循环。</para>
/// <para>释放与续期都用 Lua 脚本先比对 token, 避免误删/误续别人的锁。</para>
/// </summary>
internal sealed class RedisLockEntry
{
    /// <summary>
    /// 释放脚本: 值是自己的才删除, 返回 1 表示删掉了
    /// </summary>
    internal const string ReleaseScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    /// <summary>
    /// 续期脚本: 值是自己的才重设过期时间, 返回 1 表示续期成功
    /// </summary>
    internal const string ExtendScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('pexpire', KEYS[1], ARGV[2]) else return 0 end";

    private readonly IDatabase _database;
    private readonly RedisKey _key;
    private readonly RedisValue _token;
    private readonly CancellationTokenSource _lostSource = new();
    private readonly List<DistributedLockFlowNode> _holds = [];
    private LockRenewal? _renewal;
    private int _released;

    internal RedisLockEntry(IDatabase database, RedisKey key, RedisValue token)
    {
        _database = database;
        _key = key;
        _token = token;
    }

    /// <summary>
    /// 锁键(已叠加前缀)
    /// </summary>
    internal RedisKey Key => _key;

    /// <summary>
    /// 持有栈的门锁: 重入判定与压栈在同一把锁里完成, 并行分支同时到达时只有一个能进去
    /// </summary>
    internal object Gate { get; } = new();

    /// <summary>
    /// 当前持有层数; 归零表示已完全释放
    /// </summary>
    internal int HoldCount
    {
        get
        {
            lock (Gate)
            {
                return _holds.Count;
            }
        }
    }

    /// <summary>
    /// 锁是否已丢失: 续期失败、键被改写或已经释放
    /// </summary>
    internal bool IsLockLost => _lostSource.IsCancellationRequested || (_renewal?.IsLockLost ?? false);

    /// <summary>
    /// 锁丢失信号: 已经被判定丢锁(续期失败/已释放)时立刻反映, 否则以续期循环的为准
    /// </summary>
    internal CancellationToken LostToken => _lostSource.IsCancellationRequested
        ? _lostSource.Token
        : (_renewal?.LostToken ?? _lostSource.Token);

    /// <summary>
    /// 挂上续期循环; 由锁实现在拿锁成功后调用
    /// </summary>
    /// <param name="renewal">续期循环; 显式租期的抢锁不挂, 由后端到点自动释放</param>
    internal void AttachRenewal(LockRenewal? renewal)
    {
        _renewal = renewal;
    }

    /// <summary>
    /// 尝试按重入处理: 只有「调用链上持有的节点正好是当前最内层持有者」才允许, 否则说明是并行分支, 应回去重抢
    /// </summary>
    /// <param name="heldNode">调用链上持有的节点</param>
    /// <param name="node">本次加锁的节点</param>
    /// <returns>是否按重入处理</returns>
    internal bool TryEnterReentrant(DistributedLockFlowNode heldNode, DistributedLockFlowNode node)
    {
        lock (Gate)
        {
            if (_released != 0 || _holds.Count == 0 || !ReferenceEquals(_holds[^1], heldNode))
            {
                return false;
            }

            _holds.Add(node);

            return true;
        }
    }

    /// <summary>
    /// 把节点压入持有栈(首抢成功时调用)
    /// </summary>
    /// <param name="node">本次加锁的节点</param>
    internal void Push(DistributedLockFlowNode node)
    {
        lock (Gate)
        {
            _holds.Add(node);
        }
    }

    /// <summary>
    /// 从持有栈摘掉节点
    /// </summary>
    /// <param name="node">要摘掉的节点</param>
    /// <returns>剩余持有层数</returns>
    internal int RemoveHold(DistributedLockFlowNode node)
    {
        lock (Gate)
        {
            _holds.Remove(node);

            return _holds.Count;
        }
    }

    /// <summary>
    /// 重设租期(不是延长): 先比对 token, 再 <c>pexpire</c>; 比对失败即判定丢锁
    /// </summary>
    /// <param name="leaseTime">新的租期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否续期成功</returns>
    internal async Task<bool> TryExtendAsync(TimeSpan leaseTime, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested || leaseTime <= TimeSpan.Zero)
        {
            return false;
        }

        RedisResult result = await _database
            .ScriptEvaluateAsync(ExtendScript, [_key], [_token, (long)leaseTime.TotalMilliseconds])
            .ConfigureAwait(false);

        bool extended = (long)result == 1;

        if (!extended)
        {
            MarkLost();
        }

        return extended;
    }

    /// <summary>
    /// 彻底释放: 停续期并删键(调用方保证持有栈已空, 只调一次)
    /// </summary>
    /// <returns>异步任务</returns>
    internal async Task ReleaseAsync()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
        {
            return;
        }

        LockRenewal? renewal = _renewal;
        _renewal = null;

        if (renewal is not null)
        {
            await renewal.DisposeAsync().ConfigureAwait(false);
        }

        try
        {
            // 只删自己写进去的那把锁; 键已过期或被别人抢到时脚本什么都不做
            await _database.ScriptEvaluateAsync(ReleaseScript, [_key], [_token]).ConfigureAwait(false);
        }
        finally
        {
            MarkLost();
        }
    }

    /// <summary>
    /// 这把锁在 Redis 里是否还属于自己: 键存在、值仍是本 entry 的 token, 且没被判过丢锁。
    /// <para>重入前必须问一次: 显式租期没有看门狗, 键可能早就过期, 此时本地表里的 entry 只是僵尸记录,
    /// 若照着僵尸记录放行重入, 本进程与别的实例就会同时持锁。</para>
    /// </summary>
    /// <returns>仍持有返回 true</returns>
    internal async Task<bool> IsAliveAsync()
    {
        if (IsLockLost || Volatile.Read(ref _released) != 0)
        {
            return false;
        }

        RedisValue current = await _database.StringGetAsync(_key).ConfigureAwait(false);

        return current == _token;
    }

    /// <summary>
    /// 放弃这把锁: 键已经不属于自己(过期或被别人抢走), 停续期并标记丢锁。
    /// <para>刻意<strong>不跑删除脚本</strong> —— 此刻键可能正被别的实例持有, 删了就是替别人解锁;
    /// 同时把 <c>_released</c> 置位, 保证原句柄之后 Dispose 也不会再碰这把键。</para>
    /// </summary>
    /// <returns>异步任务</returns>
    internal async ValueTask AbandonAsync()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
        {
            return;
        }

        LockRenewal? renewal = Interlocked.Exchange(ref _renewal, null);

        if (renewal is not null)
        {
            await renewal.DisposeAsync().ConfigureAwait(false);
        }

        lock (Gate)
        {
            _holds.Clear();
        }

        MarkLost();
    }

    /// <summary>
    /// 标记丢锁: 只触发一次
    /// </summary>
    private void MarkLost()
    {
        if (_lostSource.IsCancellationRequested)
        {
            return;
        }

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
