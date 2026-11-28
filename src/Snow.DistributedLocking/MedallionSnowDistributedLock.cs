using Medallion.Threading;
using Snow.DependencyInjection;

namespace Snow.DistributedLocking;

/// <summary>
/// 基于 Medallion 的分布式锁实现(与 ABP 的 <c>MedallionAbpDistributedLock</c> 一致):
/// 拿 <see cref="IDistributedLockProvider"/> 提供的锁, 具体后端(Redis / Postgres / SqlServer …)由宿主注册, 与 ABP 一致。
/// <para>注册为 <c>ReplaceServices = true</c> 顶替进程内的 <see cref="LocalSnowDistributedLock"/>, 因此引入后端包后自动变成跨实例锁。</para>
/// </summary>
[TransientService(ReplaceServices = true)]
public class MedallionSnowDistributedLock : IDistributedLock
{
    /// <summary>
    /// Medallion 的锁提供者
    /// </summary>
    protected IDistributedLockProvider DistributedLockProvider { get; }

    /// <summary>
    /// key 归一化服务
    /// </summary>
    protected IDistributedLockKeyNormalizer DistributedLockKeyNormalizer { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="distributedLockProvider">Medallion 锁提供者</param>
    /// <param name="distributedLockKeyNormalizer">key 归一化服务</param>
    public MedallionSnowDistributedLock(
        IDistributedLockProvider distributedLockProvider,
        IDistributedLockKeyNormalizer distributedLockKeyNormalizer)
    {
        ArgumentNullException.ThrowIfNull(distributedLockProvider);
        ArgumentNullException.ThrowIfNull(distributedLockKeyNormalizer);

        DistributedLockProvider = distributedLockProvider;
        DistributedLockKeyNormalizer = distributedLockKeyNormalizer;
    }

    /// <inheritdoc />
    public async Task<IDistributedLockHandle?> TryAcquireAsync(
        string name,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));

        string key = DistributedLockKeyNormalizer.NormalizeKey(name);

        IDistributedSynchronizationHandle? handle = await DistributedLockProvider
            .TryAcquireLockAsync(key, timeout, cancellationToken)
            .ConfigureAwait(false);

        return handle is null ? null : new MedallionSnowDistributedLockHandle(handle);
    }
}
