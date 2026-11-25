using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Snow.Aop;
using Snow.Expressions;

namespace Snow.DistributedLocking;

/// <summary>
/// 方法级分布式锁注解(对应 Java 的 <c>@DistributedLock</c>): 进入方法前按 key 拿锁, 方法返回或抛异常时释放。
/// <para>key 由 <see cref="Prefix"/> 与 <see cref="Key"/> 用 ":" 拼接; <see cref="Key"/> 与 <see cref="Condition"/>
/// 按 C# 表达式交给 <see cref="ExpressionEngine"/> 求值(可用参数名 / p0 / a0 / root.methodName / root.args)。</para>
/// <para>续期(看门狗)由锁实现负责: 不设 <see cref="LeaseSeconds"/> 时, 拿到锁后就按
/// <see cref="DistributedLockWatchdog.LeaseTime"/> 的三分之一节奏自动续期, 长任务不会因为超过租期而中途失锁;
/// 设了 <see cref="LeaseSeconds"/> 则固定租期不续期(对齐 Redisson 的 <c>lock(leaseTime, unit)</c>)。
/// 锁真的丢了句柄的 <see cref="ISnowDistributedLockHandle.HandleLostToken"/> 会被取消, 开了 <see cref="ThrowOnLost"/> 则抛异常。</para>
/// </summary>
/// <example>
/// <code>
/// [DistributedLock("\"deduct:\" + id", Prefix = "stock", Condition = "id &gt; 0", WaitSeconds = 5)]
/// public async Task DeductAsync(long id) { ... }
///
/// // 固定 30 秒租期、不续期: 到点由后端释放, 不管方法是否还在跑
/// [DistributedLock("\"report:\" + id", LeaseSeconds = 30)]
/// public async Task BuildReportAsync(long id) { ... }
/// </code>
/// </example>
/// <para>拿不到锁时抛 <see cref="TimeoutException"/>; <see cref="WaitSeconds"/> 默认 -1 表示<b>一直等待</b>
/// (对齐 Redisson 的 <c>lock()</c>), 为 0 表示不等待、立即判失败, 正数表示最多等这么多秒。</para>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DistributedLockAttribute : SnowInterceptorAttribute
{
    /// <summary>
    /// 构造器: 可只写 key, 也可只写 <see cref="Prefix"/> 等具名属性
    /// </summary>
    /// <param name="key">锁 key 表达式</param>
    public DistributedLockAttribute(string? key = null)
    {
        Key = key;
    }

    /// <summary>
    /// 锁 key 表达式(如 ""order:" + id"); 为空时用「类名_方法名」
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// 锁 key 前缀, 与 <see cref="Key"/> 用 ":" 拼接; 为空时只用 <see cref="Key"/>
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// 加锁条件表达式, 求值为 false 时跳过加锁直接执行方法
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// 抢锁的等待秒数, 默认 -1 表示<b>一直等待</b>, 直到拿到锁或取消令牌触发
    /// <para>对齐 Redisson 的三种入口: -1(默认) = <c>lock()</c> 一直等; 0 = <c>tryLock()</c> 抢一次;
    /// 正数 = <c>tryLock(waitTime, unit)</c> 最多等这么多秒。</para>
    /// <para>特性参数只认非可空的 <see cref="double"/>, 所以「一直等」用 -1 表达而不是 null。</para>
    /// </summary>
    public double WaitSeconds { get; set; } = -1;

    /// <summary>
    /// 本次持锁的租期秒数(对应 Redisson 的 <c>lock(leaseTime, unit)</c>); 0(默认)表示用
    /// <see cref="DistributedLockWatchdog.LeaseTime"/> 并由看门狗自动续期, 方法跑多久锁就保多久
    /// <para>设了非零值即固定租期、<b>不续期</b>: 到点锁由后端自动释放, 即使方法还在跑。适合「最多允许占用 N 秒」的防重场景。</para>
    /// </summary>
    public double LeaseSeconds { get; set; }

    /// <summary>
    /// 锁在方法执行期间丢失时是否抛 <see cref="DistributedLockLostException"/>, 默认只记日志
    /// </summary>
    public bool ThrowOnLost { get; set; }

    /// <inheritdoc />
    /// <param name="context">上下文</param>
    /// <param name="next">后续执行委托</param>
    /// <param name="cancellationToken">取消令牌</param>
    public override async Task InvokeAsync(AspectContext context, AspectDelegate next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (
            !string.IsNullOrWhiteSpace(Condition)
            && !ExpressionEngine.EvaluateBoolean(
                Condition,
                ExpressionContextFactory.Create(context)
            )
        )
        {
            await next(context, cancellationToken).ConfigureAwait(false);
            return;
        }

        string key = ResolveKey(context);
        // 不写(-1)或其它负数 = 一直等(对齐 Redisson 的 lock()); 0 = 抢一次; 正数 = 最多等这么多秒
        TimeSpan? timeout =
            WaitSeconds < 0 ? null
            : WaitSeconds > 0 ? TimeSpan.FromSeconds(WaitSeconds)
            : TimeSpan.Zero;
        TimeSpan? leaseTime = LeaseSeconds > 0 ? TimeSpan.FromSeconds(LeaseSeconds) : null;

        // 配置前缀统一由 IDistributedLockKeyNormalizer 叠加(本地锁与 Redis 锁都走它), 这里只拼注解自己的 Prefix 与 Key
        ISnowDistributedLock distributedLock =
            context.ServiceProvider.GetRequiredService<ISnowDistributedLock>();
        // cancellationToken 由适配器从方法签名里取出后交给整条链, 所以这里的等待天然可以被叫停:
        // 无限等待没有它就是一个永远退不出来的阻塞
        ISnowDistributedLockHandle? handle = await distributedLock
            .TryAcquireAsync(key, timeout, leaseTime, cancellationToken)
            .ConfigureAwait(false);

        if (handle is null)
        {
            // 拿到 null 可能是因为超时, 也可能是等待被取消; 取消按取消处理, 不要伪装成超时
            cancellationToken.ThrowIfCancellationRequested();

            // 无限等待不会自己超时, 走到这里只可能是取消令牌触发了
            string reason =
                timeout is null || timeout == Timeout.InfiniteTimeSpan
                    ? "取消前一直没拿到"
                    : "等待 " + timeout.Value.TotalSeconds.ToString("0.###") + " 秒后仍未拿到";
            throw new TimeoutException(
                "获取分布式锁失败: 锁 key 为 "
                    + key
                    + ", "
                    + reason
                    + ", 方法为 "
                    + DescribeMethod(context)
                    + "。"
            );
        }

        try
        {
            await next(context, cancellationToken).ConfigureAwait(false);

            // 续期与丢失判定都在锁实现里完成, 句柄的丢失令牌被取消就说明这把锁已经不是自己的了
            if (ThrowOnLost && handle.IsLockLost)
            {
                throw new DistributedLockLostException(
                    key,
                    "分布式锁在方法执行期间丢失: 锁 key 为 "
                        + key
                        + ", 方法为 "
                        + DescribeMethod(context)
                        + "。"
                );
            }
        }
        finally
        {
            // 释放句柄时一并停掉看门狗(带看门狗的句柄自己处理)
            await handle.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 计算最终锁 key: 表达式求值, 空则退回默认 key, 再按需拼前缀
    /// </summary>
    /// <param name="context">方法调用上下文</param>
    /// <returns>锁 key</returns>
    private string ResolveKey(AspectContext context)
    {
        string value = string.IsNullOrWhiteSpace(Key)
            ? BuildDefaultKey(context)
            : ExpressionEngine.EvaluateString(Key, ExpressionContextFactory.Create(context));

        if (string.IsNullOrWhiteSpace(value))
        {
            value = BuildDefaultKey(context);
        }

        return string.IsNullOrWhiteSpace(Prefix) ? value : Prefix + ":" + value;
    }


    /// <summary>
    /// 默认 key: 「类名_方法名」
    /// </summary>
    /// <param name="context">方法调用上下文</param>
    /// <returns>默认锁 key</returns>
    private static string BuildDefaultKey(AspectContext context)
    {
        Type? ownerType = context.TargetObject?.GetType() ?? context.Method.DeclaringType;

        return (ownerType?.Name ?? "unknown") + "_" + context.Method.Name;
    }

    /// <summary>
    /// 描述被拦截方法, 用于异常消息
    /// </summary>
    /// <param name="context">方法调用上下文</param>
    /// <returns>类型名.方法名</returns>
    private static string DescribeMethod(AspectContext context)
    {
        StringBuilder builder = new();
        builder
            .Append(context.Method.DeclaringType?.FullName ?? "unknown")
            .Append('.')
            .Append(context.Method.Name);

        return builder.ToString();
    }
}
