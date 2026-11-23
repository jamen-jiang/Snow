namespace Snow.DistributedLocking;

/// <summary>
/// 进程内锁句柄(对应 ABP 的 LocalAbpDistributedLockHandle): 释放时归还信号量的一层。
/// <para>进程内锁没有租期概念, 只要没释放就一直有效, 因此不支持续期, 也不会丢锁。</para>
/// </summary>
public sealed class LocalDistributedLockHandle : IDistributedLockHandle
{
    private readonly LocalDistributedLock.LocalLockEntry _entry;
    private readonly DistributedLockFlowNode _node;
    private readonly Action _onRelease;
    private int _disposed;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="entry">锁的内部状态(信号量与持有栈)</param>
    /// <param name="node">本次加锁在调用链上的节点</param>
    /// <param name="onRelease">释放时执行的动作(归还一层)</param>
    internal LocalDistributedLockHandle(LocalDistributedLock.LocalLockEntry entry, DistributedLockFlowNode node, Action onRelease)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(onRelease);

        _entry = entry;
        _node = node;
        _onRelease = onRelease;
    }

    /// <inheritdoc />
    public CancellationToken HandleLostToken => CancellationToken.None;

    /// <inheritdoc />
    public bool IsLockLost => false;

    /// <inheritdoc />
    public bool IsHeldByCurrentFlow => !_node.Released;

    /// <inheritdoc />
    public int HoldCount => _entry.HoldCount;

    /// <inheritdoc />
    public bool CanExtend => false;

    /// <inheritdoc />
    public Task<bool> TryExtendAsync(TimeSpan leaseTime, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _onRelease();
        }

        return default;
    }
}
