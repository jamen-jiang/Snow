using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using StackExchange.Redis;

namespace Snow.DistributedLocking;

/// <summary>
/// 基于 Redis 的分布式锁(自研, 不依赖任何第三方锁库): 拿锁用 <c>SET key token NX PX 租期</c>,
/// 释放与续期用 Lua 脚本保证「只操作自己那把锁」。
/// <para>可重入: 同一条调用链顺序嵌套对同名锁再次抢, 直接复用已持有的 entry 并压一层(重入前会用一次 GET
/// 确认键仍归自己, 免得把已过期的僵尸记录当成还持有), 不会自己把自己锁死;
/// <c>Task.Run</c> 起的并行分支不算重入, 会正常去抢 Redis 的锁。逐层释放, 归零时才停续期并删键。</para>
/// <para>拿锁成功后立即启动看门狗(对应 Redisson 在 <c>lock()</c> 里启 <c>ExpirationRenewal</c>),
/// 业务跑多久锁就保多久。</para>
/// <para>标 <see cref="DependencyAttribute"/>(ReplaceServices = true) 与 <see cref="ISingletonDependency"/>,
/// 由约定注册顶替默认的 <see cref="LocalDistributedLock"/>。</para>
/// </summary>
[Dependency(ReplaceServices = true)]
public class RedisDistributedLock : IDistributedLock, ISingletonDependency
{
    /// <summary>
    /// 抢锁失败后的重试间隔: 有等待上限时按这个节奏轮询
    /// </summary>
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// 本进程当前持有的锁: 键 → 内部状态。互斥保证同一个键同一时刻只有一条调用链持有, 所以按键存一份就够。
    /// </summary>
    private readonly ConcurrentDictionary<string, RedisLockEntry> _entries = new(StringComparer.Ordinal);

    /// <summary>
    /// Redis 连接(由 <c>SnowDistributedLockRedisModule</c> 注册的单例)
    /// </summary>
    protected IConnectionMultiplexer ConnectionMultiplexer { get; }

    /// <summary>
    /// 锁配置(key 前缀)
    /// </summary>
    protected DistributedLockOptions Options { get; }

    /// <summary>
    /// 看门狗: 拿锁成功后立即由它接管续期
    /// </summary>
    protected DistributedLockWatchdog DistributedLockWatchdog { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="connectionMultiplexer">Redis 连接</param>
    /// <param name="options">锁配置</param>
    /// <param name="distributedLockWatchdog">看门狗</param>
    public RedisDistributedLock(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<DistributedLockOptions> options,
        DistributedLockWatchdog distributedLockWatchdog)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(distributedLockWatchdog);

        ConnectionMultiplexer = connectionMultiplexer;
        Options = options.Value;
        DistributedLockWatchdog = distributedLockWatchdog;
    }

    /// <inheritdoc />
    /// <remarks>
    /// 刻意不写成 async 方法: 调用链节点必须在同步段写入 <see cref="DistributedLockFlow"/>,
    /// 若在异步方法内部写, 写的是该方法自己的上下文副本, 返回后调用方读不到, 可重入永远判不出来。
    /// </remarks>
    public Task<IDistributedLockHandle?> TryAcquireAsync(
        string name,
        TimeSpan? timeout = null,
        TimeSpan? leaseTime = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string key = Options.ResolveKey(name);
        DistributedLockFlowNode? heldNode = DistributedLockFlow.FindHeld(this, key);
        DistributedLockFlowNode node = DistributedLockFlow.Begin(this, key);

        return TryAcquireCoreAsync(key, node, heldNode, timeout, leaseTime, cancellationToken);
    }

    /// <summary>
    /// 抢锁主体
    /// </summary>
    /// <param name="key">已规范化的锁 key</param>
    /// <param name="node">本次加锁在调用链上的节点</param>
    /// <param name="heldNode">调用链上已持有的同名锁节点(没有则为 null)</param>
    /// <param name="timeout">等待上限</param>
    /// <param name="leaseTime">本次租期; 为 null 时用默认租期并启动看门狗, 有值时固定租期且不续期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁句柄; 拿不到时返回 null</returns>
    private async Task<IDistributedLockHandle?> TryAcquireCoreAsync(
        string key,
        DistributedLockFlowNode node,
        DistributedLockFlowNode? heldNode,
        TimeSpan? timeout,
        TimeSpan? leaseTime,
        CancellationToken cancellationToken)
    {
        // 重入: 调用链上持有的正是这把锁最内层的持有者, 直接压一层, 不再发 SET NX。
        // 判定与压栈都在 entry 的门锁内完成, 并行分支同时到达时只有一个能进去, 另一个老老实实去抢。
        // 但先得用一次 GET 确认键还归自己: 显式租期没有看门狗, 键可能早已过期,
        // 此时表里的 entry 只是僵尸记录, 照它放行重入就会让本进程与别的实例同时持锁。
        if (heldNode is not null && _entries.TryGetValue(key, out RedisLockEntry? heldEntry))
        {
            if (await heldEntry.IsAliveAsync().ConfigureAwait(false) && heldEntry.TryEnterReentrant(heldNode, node))
            {
                return CreateHandle(heldEntry, key, node);
            }

            // 键已经不属于自己: 摘掉僵尸记录、停续期、标丢锁, 然后当作没持有一路往下重抢
            _entries.TryRemove(new KeyValuePair<string, RedisLockEntry>(key, heldEntry));

            await heldEntry.AbandonAsync().ConfigureAwait(false);
        }

        // 租期语义对齐 Redisson: 显式租期优先; 显式租期时不续期(到点由 Redis 自动释放)
        bool hasExplicitLease = leaseTime.HasValue && leaseTime.Value > TimeSpan.Zero;
        TimeSpan effectiveLease = hasExplicitLease ? leaseTime!.Value : DistributedLockWatchdog.LeaseTime;

        // 每次抢锁用新的随机 token: 释放/续期时用它证明「键里的值是我写的」
        RedisValue token = Guid.NewGuid().ToString("N");
        IDatabase database = ConnectionMultiplexer.GetDatabase();
        // 不传 timeout 就等于一直等(对齐 Redisson 的 lock()); 要"抢一次"必须显式传 TimeSpan.Zero
        bool infiniteWait = timeout is null || timeout == Timeout.InfiniteTimeSpan;
        DateTime deadline = infiniteWait
            ? DateTime.MaxValue
            : DateTime.UtcNow + (timeout!.Value > TimeSpan.Zero ? timeout.Value : TimeSpan.Zero);

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // 取消按「没抢到锁」处理, 与返回 null 的契约一致
                DistributedLockFlow.Release(node);

                return null;
            }

            // 一条命令完成「不存在才写入 + 带租期」, 原子且不会覆盖别人的锁
            if (await database.StringSetAsync(key, token, effectiveLease, When.NotExists).ConfigureAwait(false))
            {
                RedisLockEntry entry = new(database, key, token);

                entry.Push(node);
                _entries[key] = entry;

                RedisDistributedLockHandle lockHandle = CreateHandle(entry, key, node);

                // 显式租期表示「最多占用这么久」, 与 Redisson 的 lock(leaseTime, unit) 一致: 不续期。
                // 续不续期只看这一次有没有传租期, 没有全局开关(对齐 Redisson)。
                if (!hasExplicitLease)
                {
                    entry.AttachRenewal(DistributedLockWatchdog.Start(lockHandle, key));
                }

                return lockHandle;
            }

            TimeSpan delay;

            if (infiniteWait)
            {
                delay = RetryInterval;
            }
            else
            {
                TimeSpan remaining = deadline - DateTime.UtcNow;

                if (remaining <= TimeSpan.Zero)
                {
                    DistributedLockFlow.Release(node);

                    return null;
                }

                delay = remaining < RetryInterval ? remaining : RetryInterval;
            }

            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                DistributedLockFlow.Release(node);

                return null;
            }
        }
    }

    /// <summary>
    /// 造一个句柄
    /// </summary>
    /// <param name="entry">锁的内部状态</param>
    /// <param name="key">已规范化的锁 key</param>
    /// <param name="node">本次加锁在调用链上的节点</param>
    /// <returns>锁句柄</returns>
    private RedisDistributedLockHandle CreateHandle(RedisLockEntry entry, string key, DistributedLockFlowNode node)
    {
        return new RedisDistributedLockHandle(entry, node, () => ReleaseAsync(entry, key, node));
    }

    /// <summary>
    /// 释放一层: 还有外层就只从持有栈摘掉自己, 栈空了才停续期、删键并从表里摘掉
    /// </summary>
    /// <param name="entry">锁的内部状态</param>
    /// <param name="key">已规范化的锁 key</param>
    /// <param name="node">要释放的调用链节点</param>
    /// <returns>异步任务</returns>
    private async ValueTask ReleaseAsync(RedisLockEntry entry, string key, DistributedLockFlowNode node)
    {
        DistributedLockFlow.Release(node);

        if (entry.RemoveHold(node) > 0)
        {
            return;
        }

        _entries.TryRemove(new KeyValuePair<string, RedisLockEntry>(key, entry));

        await entry.ReleaseAsync().ConfigureAwait(false);
    }
}
