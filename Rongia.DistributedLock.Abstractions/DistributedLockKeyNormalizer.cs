using Microsoft.Extensions.Options;
using Rongia.DependencyInjection;

namespace Rongia.DistributedLock;

/// <summary>
/// 默认的锁 key 规范化实现(对应 ABP 的 DistributedLockKeyNormalizer):配置前缀 + 业务名
/// <para>配置通过 <see cref="IOptions{TOptions}"/> 注入,实现 <see cref="ITransientDependency"/> 由约定注册自动登记。</para>
/// </summary>
public class DistributedLockKeyNormalizer : IDistributedLockKeyNormalizer, ITransientDependency
{
    /// <summary>
    /// 锁配置
    /// </summary>
    protected DistributedLockOptions Options { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">锁配置</param>
    public DistributedLockKeyNormalizer(IOptions<DistributedLockOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options.Value;
    }

    /// <inheritdoc />
    public virtual string NormalizeKey(string name)
    {
        return Options.KeyPrefix + name;
    }
}
