using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁看门狗(对应 Redisson 的 lock watchdog): 持锁期间按「租期三分之一」续期, 业务跑多久锁就保多久。
/// <para>每次 <see cref="Start"/> 为一个锁句柄起一条独立的续期循环(<see cref="LockRenewal"/>); 可重入的锁在重入时
/// 共享同一个句柄, 所以同一把锁同一时刻只会有这一条循环。</para>
/// <para>没有全局开关(对齐 Redisson: 它只有 <c>Config.lockWatchdogTimeout</c> 一个配置项, 靠不传
/// <c>leaseTime</c> 决定续不续期)。要「到点自动释放」就在这一次抢锁时传 <c>leaseTime</c>。</para>
/// </summary>
public class DistributedLockWatchdog : ISingletonDependency
{
    /// <summary>
    /// 锁的默认租期, 30 秒(对齐 Redisson 的 <c>lockWatchdogTimeout</c> 默认值)
    /// <para>也是后端多久没收到续期就自动释放的时间。</para>
    /// </summary>
    public static readonly TimeSpan LeaseTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 续期间隔: 租期的三分之一(对齐 Redisson)
    /// </summary>
    public static readonly TimeSpan RenewalInterval = TimeSpan.FromTicks(LeaseTime.Ticks / 3);

    private readonly ILogger<DistributedLockWatchdog>? _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="logger">日志</param>
    public DistributedLockWatchdog(ILogger<DistributedLockWatchdog>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 为锁句柄启动续期
    /// </summary>
    /// <param name="handle">锁句柄</param>
    /// <param name="key">锁 key, 只用于日志</param>
    /// <returns>续期循环</returns>
    public LockRenewal Start(IDistributedLockHandle handle, string key)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        LockRenewal renewal = new(handle, key, LeaseTime, RenewalInterval, _logger);
        renewal.Start();

        return renewal;
    }
}
