namespace Rongia.DistributedLock;

/// <summary>
/// 锁 key 规范化器(对应 ABP 的 IDistributedLockKeyNormalizer):把业务名转成最终存储 key
/// </summary>
public interface IDistributedLockKeyNormalizer
{
    /// <summary>
    /// 规范化锁 key
    /// </summary>
    /// <param name="name">业务传入的锁名称</param>
    /// <returns>最终使用的锁 key</returns>
    string NormalizeKey(string name);
}
