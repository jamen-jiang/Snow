using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Rongia.DependencyInjection;

namespace Rongia.DistributedLock;

/// <summary>
/// 基于 Medallion 的分布式锁(对应 ABP 的 MedallionAbpDistributedLock):
/// 具体后端由宿主的 <see cref="IDistributedLockProvider"/> 决定(Redis / Postgres / SqlServer / 文件等)。
/// <para>标 <see cref="DependencyAttribute"/>(ReplaceServices = true) 与 <see cref="ITransientDependency"/>,
/// 由约定注册替换掉默认的 <see cref="LocalDistributedLock"/>。</para>
/// </summary>
[Dependency(ReplaceServices = true)]
public class MedallionDistributedLock : IDistributedLock, ITransientDependency
{
    /// <summary>
    /// Medallion 锁提供器(后端)
    /// </summary>
    protected IDistributedLockProvider DistributedLockProvider { get; }

    /// <summary>
    /// 锁 key 规范化器
    /// </summary>
    protected IDistributedLockKeyNormalizer DistributedLockKeyNormalizer { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="distributedLockProvider">Medallion 锁提供器</param>
    /// <param name="distributedLockKeyNormalizer">锁 key 规范化器</param>
    public MedallionDistributedLock(
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
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string key = DistributedLockKeyNormalizer.NormalizeKey(name);

        IDistributedSynchronizationHandle? handle = await DistributedLockProvider.TryAcquireLockAsync(key, timeout, cancellationToken);

        return handle is null ? null : new MedallionDistributedLockHandle(handle);
    }
}
