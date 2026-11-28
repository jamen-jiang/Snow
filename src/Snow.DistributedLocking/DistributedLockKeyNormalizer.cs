using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.DistributedLocking;

/// <summary>
/// 默认的锁 key 归一化(与 ABP 的 <c>DistributedLockKeyNormalizer</c> 一致): 直接拼 <see cref="SnowDistributedLockOptions.KeyPrefix"/> 与业务名。
/// <para>选项只读一次(<see cref="IOptions{TOptions}.Value"/> 是单例), 改前缀要重启才生效, 与 ABP 一致。</para>
/// </summary>
[TransientService]
public class DistributedLockKeyNormalizer : IDistributedLockKeyNormalizer
{
    /// <summary>
    /// 锁配置(key 前缀)
    /// </summary>
    protected SnowDistributedLockOptions Options { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">锁配置</param>
    public DistributedLockKeyNormalizer(IOptions<SnowDistributedLockOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options.Value;
    }

    /// <inheritdoc />
    public virtual string NormalizeKey(string name)
    {
        return $"{Options.KeyPrefix}{name}";
    }
}