using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.DistributedLocking;

/// <summary>
/// 进程内分布式锁(对应 ABP 的 LocalAbpDistributedLock): 按 key 分配信号量, 只在本进程内互斥。
/// <para>作为默认实现, 单机部署即可工作; 多实例部署时由 <c>Snow.DistributedLocking.Redis</c> 的
/// <c>RedisDistributedLock</c> 顶替。</para>
/// <para>可重入: 同一条调用链顺序嵌套对同一把锁再次加锁不会自锁, 每层各释放一次, 归零才归还信号量;
/// <c>Task.Run</c> 起的并行分支不算重入, 会正常排队, 互斥不会被破坏。
/// key 无人使用时连同信号量一起从字典摘除, 不随 key 数量增长而泄漏。</para>
/// <para>进程内锁没有租期概念, 因此不启用看门狗, <see cref="IDistributedLockHandle.CanExtend"/> 恒为 false。</para>
/// </summary>
[SingletonService]
public class LocalDistributedLock : IDistributedLock
{
    private readonly ConcurrentDictionary<string, LocalLockEntry> _entries = new(StringComparer.Ordinal);

    /// <summary>
    /// 锁配置(前缀)
    /// </summary>
    protected DistributedLockOptions Options { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">分布式锁配置</param>
    public LocalDistributedLock(IOptions<DistributedLockOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options.Value;
    }

    /// <inheritdoc />
    /// <remarks>
    /// 刻意不写成 async 方法: 调用链节点必须在同步段写入 <see cref="DistributedLockFlow"/>,
    /// 若在异步方法内部写, 写的是该方法自己的上下文副本, 方法返回后调用方读不到, 可重入就永远判不出来。
    /// </remarks>
    public Task<IDistributedLockHandle?> TryAcquireAsync(
        string name,
        TimeSpan? timeout = null,
        TimeSpan? leaseTime = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // 进程内锁没有租期概念: leaseTime 在此被忽略, 锁一直有效到释放为止
        string key = Options.ResolveKey(name);
        DistributedLockFlowNode? heldNode = DistributedLockFlow.FindHeld(this, key);
        DistributedLockFlowNode node = DistributedLockFlow.Begin(this, key);

        return TryAcquireCoreAsync(key, node, heldNode, timeout, cancellationToken);
    }

    /// <summary>
    /// 抢锁主体
    /// </summary>
    /// <param name="key">已规范化的锁 key</param>
    /// <param name="node">本次加锁在调用链上的节点</param>
    /// <param name="heldNode">调用链上已持有的同名锁节点(没有则为 null)</param>
    /// <param name="timeout">等待上限</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁句柄; 拿不到时返回 null</returns>
    private async Task<IDistributedLockHandle?> TryAcquireCoreAsync(
        string key,
        DistributedLockFlowNode node,
        DistributedLockFlowNode? heldNode,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        // 重入: 调用链上持有的正是这把锁当前的持有者, 直接压一层, 不碰信号量。
        // 判定与压栈都在锁容器的门锁内完成, 所以并行分支同时到达时只有一个能进去, 另一个老老实实去排队。
        if (heldNode is not null
            && _entries.TryGetValue(key, out LocalLockEntry? heldEntry)
            && heldEntry.TryEnterReentrant(heldNode, node))
        {
            return CreateHandle(heldEntry, key, node);
        }

        LocalLockEntry entry = AcquireEntry(key);
        bool acquired;

        try
        {
            acquired = await entry.Semaphore.WaitAsync(NormalizeTimeout(timeout), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 取消按「没抢到锁」处理, 与 TryAcquireAsync 的契约一致(Redis 实现同样返回 null)
            ReleaseEntry(key, entry);
            DistributedLockFlow.Release(node);

            return null;
        }
        catch
        {
            // 其它异常: 归还使用者计数, 避免字典项永远留着
            ReleaseEntry(key, entry);
            DistributedLockFlow.Release(node);

            throw;
        }

        if (!acquired)
        {
            ReleaseEntry(key, entry);
            DistributedLockFlow.Release(node);

            return null;
        }

        entry.Push(node);

        return CreateHandle(entry, key, node);
    }

    /// <summary>
    /// 造一个句柄
    /// </summary>
    /// <param name="entry">锁容器</param>
    /// <param name="key">已规范化的锁 key</param>
    /// <param name="node">本次加锁在调用链上的节点</param>
    /// <returns>锁句柄</returns>
    private LocalDistributedLockHandle CreateHandle(LocalLockEntry entry, string key, DistributedLockFlowNode node)
    {
        return new LocalDistributedLockHandle(entry, node, () => Exit(entry, key, node));
    }

    /// <summary>
    /// 释放一层: 还有外层就只从持有栈摘掉自己, 栈空了才归还信号量并回收字典项
    /// </summary>
    /// <param name="entry">锁容器</param>
    /// <param name="key">已规范化的锁 key</param>
    /// <param name="node">要释放的调用链节点</param>
    private void Exit(LocalLockEntry entry, string key, DistributedLockFlowNode node)
    {
        DistributedLockFlow.Release(node);

        if (entry.RemoveHold(node) > 0)
        {
            return;
        }

        entry.Semaphore.Release();
        ReleaseEntry(key, entry);
    }

    /// <summary>
    /// 取锁对应的信号量容器并登记一个使用者; 已被回收的容器会重新创建
    /// </summary>
    /// <param name="key">已规范化的锁 key</param>
    /// <returns>锁容器</returns>
    private LocalLockEntry AcquireEntry(string key)
    {
        while (true)
        {
            LocalLockEntry entry = _entries.GetOrAdd(key, static _ => new LocalLockEntry());

            lock (entry.Gate)
            {
                if (entry.Removed)
                {
                    continue;
                }

                entry.Users++;

                return entry;
            }
        }
    }

    /// <summary>
    /// 归还一个使用者; 归零时把容器从字典摘除
    /// </summary>
    /// <param name="key">已规范化的锁 key</param>
    /// <param name="entry">锁容器</param>
    private void ReleaseEntry(string key, LocalLockEntry entry)
    {
        lock (entry.Gate)
        {
            entry.Users--;

            if (entry.Users > 0)
            {
                return;
            }

            entry.Removed = true;
            _entries.TryRemove(new KeyValuePair<string, LocalLockEntry>(key, entry));
        }
    }

    /// <summary>
    /// 统一等待语义: 不传或 <see cref="Timeout.InfiniteTimeSpan"/> 表示一直等, 零与负数表示只抢一次
    /// </summary>
    /// <param name="timeout">调用方传入的等待时长</param>
    /// <returns>可直接传给信号量的等待时长</returns>
    private static TimeSpan NormalizeTimeout(TimeSpan? timeout)
    {
        // 不传即一直等(与 Redisson 的 lock() 一致), 这是默认行为, 不是"抢一次"
        if (timeout is null || timeout == Timeout.InfiniteTimeSpan)
        {
            return Timeout.InfiniteTimeSpan;
        }

        return timeout.Value > TimeSpan.Zero ? timeout.Value : TimeSpan.Zero;
    }

    /// <summary>
    /// 一把锁在本进程内的信号量、使用者计数与持有栈
    /// </summary>
    internal sealed class LocalLockEntry
    {
        private readonly List<DistributedLockFlowNode> _holds = [];

        internal SemaphoreSlim Semaphore { get; } = new(1, 1);

        internal object Gate { get; } = new();

        internal int Users { get; set; }

        internal bool Removed { get; set; }

        /// <summary>
        /// 当前持有层数
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
        /// 尝试按重入处理: 只有「调用链上持有的节点正好是当前最内层持有者」才允许, 否则说明是并行分支, 应去排队
        /// </summary>
        /// <param name="heldNode">调用链上持有的节点</param>
        /// <param name="node">本次加锁的节点</param>
        /// <returns>是否按重入处理</returns>
        internal bool TryEnterReentrant(DistributedLockFlowNode heldNode, DistributedLockFlowNode node)
        {
            lock (Gate)
            {
                if (Removed || _holds.Count == 0 || !ReferenceEquals(_holds[^1], heldNode))
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
    }
}
