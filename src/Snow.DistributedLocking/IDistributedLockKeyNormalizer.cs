namespace Snow.DistributedLocking;

/// <summary>
/// 锁 key 归一化(对应 ABP 的 <c>IDistributedLockKeyNormalizer</c>): 把业务锁名变成最终 key。
/// <para>各实现(本地锁 / Redis 锁)不自己拼前缀, 一律走这个服务, 换后端时前缀规则只有一处。</para>
/// </summary>
public interface IDistributedLockKeyNormalizer
{
    /// <summary>
    /// 归一化锁 key: 叠加 <see cref="SnowDistributedLockOptions.KeyPrefix"/>
    /// </summary>
    /// <param name="name">业务传入的锁名称</param>
    /// <returns>最终锁 key</returns>
    string NormalizeKey(string name);
}
