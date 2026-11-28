using Snow.DistributedLocking;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 分布式锁测试(ABP 同形): 进程内锁的互斥/释放/超时、不同 key 互不阻塞、key 前缀归一化、空实现。
/// <para>与 ABP 一样, 锁只有 <see cref="IDistributedLock.TryAcquireAsync"/> 一个入口, 没有注解层, 也不做重入。</para>
/// </summary>
public sealed class DistributedLockingTests
{
    /// <summary>
    /// 同一把锁同时只能被一个调用方持有, 释放后可以再抢到
    /// </summary>
    [Fact]
    public async Task LocalLock_IsMutuallyExclusive()
    {
        LocalSnowDistributedLock locker = CreateLocalLock();

        IDistributedLockHandle first = (await locker.TryAcquireAsync("mutual", TimeSpan.Zero))!;

        Assert.Null(await locker.TryAcquireAsync("mutual", TimeSpan.Zero));

        await first.DisposeAsync();

        IDistributedLockHandle? second = await locker.TryAcquireAsync("mutual", TimeSpan.Zero);

        Assert.NotNull(second);
        await second!.DisposeAsync();
    }

    /// <summary>
    /// 不同 key 互不影响
    /// </summary>
    [Fact]
    public async Task LocalLock_DifferentKeys_DoNotBlockEachOther()
    {
        LocalSnowDistributedLock locker = CreateLocalLock();

        IDistributedLockHandle a = (await locker.TryAcquireAsync("key-a", TimeSpan.Zero))!;
        IDistributedLockHandle? b = await locker.TryAcquireAsync("key-b", TimeSpan.Zero);

        Assert.NotNull(b);

        await a.DisposeAsync();
        await b!.DisposeAsync();
    }

    /// <summary>
    /// 指定等待上限仍拿不到时返回 null
    /// </summary>
    [Fact]
    public async Task LocalLock_WaitTimeout_ReturnsNull()
    {
        LocalSnowDistributedLock locker = CreateLocalLock();

        IDistributedLockHandle held = (await locker.TryAcquireAsync("timeout", TimeSpan.Zero))!;

        IDistributedLockHandle? blocked = await locker.TryAcquireAsync("timeout", TimeSpan.FromMilliseconds(50));

        Assert.Null(blocked);

        await held.DisposeAsync();
    }

    /// <summary>
    /// 空实现永远拿得到锁
    /// </summary>
    [Fact]
    public async Task NullLock_AlwaysAcquires()
    {
        NullSnowDistributedLock locker = new();

        await using IDistributedLockHandle? handle = await locker.TryAcquireAsync("anything");

        Assert.NotNull(handle);
    }

    /// <summary>
    /// key 归一化按 ABP 规则直接拼前缀
    /// </summary>
    [Fact]
    public void KeyNormalizer_AppliesKeyPrefix()
    {
        DistributedLockKeyNormalizer normalizer = new(
            Microsoft.Extensions.Options.Options.Create(new SnowDistributedLockOptions { KeyPrefix = "app-" }));

        Assert.Equal("app-order-1", normalizer.NormalizeKey("order-1"));
    }

    /// <summary>
    /// 造一个不经过 DI 的进程内锁
    /// </summary>
    /// <returns>进程内锁实例</returns>
    private static LocalSnowDistributedLock CreateLocalLock()
    {
        return new LocalSnowDistributedLock(
            new DistributedLockKeyNormalizer(Microsoft.Extensions.Options.Options.Create(new SnowDistributedLockOptions())));
    }
}
