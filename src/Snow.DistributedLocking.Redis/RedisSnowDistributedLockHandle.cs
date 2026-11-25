namespace Snow.DistributedLocking;

/// <summary>
/// Redis 锁句柄: 一次抢锁(或一次重入)对应一个句柄, 共享同一个内部 entry。
/// <para>可重入: 同一条调用链顺序嵌套对同名锁再抢一次会拿到共享 entry 的新句柄, 每层各释放一次, 归零才真正删键;
/// <c>Task.Run</c> 起的并行分支不算重入, 会正常去抢 Redis 的锁, 互斥不会被破坏。</para>
/// <para>键值被别人改写(过期后被别人抢到)时, 续期脚本返回 0, 句柄随即通过 <see cref="IsLockLost"/> 与
/// <see cref="HandleLostToken"/> 暴露丢锁。</para>
/// </summary>
public class RedisSnowDistributedLockHandle : ISnowDistributedLockHandle
{
    private readonly RedisLockEntry _entry;
    private readonly DistributedLockFlowNode _node;
    private readonly Func<ValueTask> _onRelease;
    private int _disposed;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="entry">锁的内部状态(键名、token、持有栈)</param>
    /// <param name="node">本次加锁在调用链上的节点</param>
    /// <param name="onRelease">释放回调: 由锁实现摘掉一层, 归零时删键</param>
    internal RedisSnowDistributedLockHandle(RedisLockEntry entry, DistributedLockFlowNode node, Func<ValueTask> onRelease)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(onRelease);

        _entry = entry;
        _node = node;
        _onRelease = onRelease;
    }

    /// <summary>
    /// 锁已丢失(续期时发现键不再是自己的)
    /// </summary>
    public bool IsLockLost => _entry.IsLockLost;

    /// <inheritdoc />
    public CancellationToken HandleLostToken => _entry.LostToken;

    /// <inheritdoc />
    public bool IsHeldByCurrentFlow => !_node.Released;

    /// <inheritdoc />
    public int HoldCount => _entry.HoldCount;

    /// <inheritdoc />
    public bool CanExtend => true;

    /// <inheritdoc />
    public Task<bool> TryExtendAsync(TimeSpan leaseTime, CancellationToken cancellationToken = default)
    {
        return _entry.TryExtendAsync(leaseTime, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _onRelease().ConfigureAwait(false);
    }
}
