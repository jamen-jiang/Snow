namespace Snow.DistributedLocking;

/// <summary>
/// 锁丢失异常: 看门狗发现锁已不在手里(续期失败、连接断开、锁被外力删除)时抛出
/// </summary>
public sealed class DistributedLockLostException : Exception
{
    /// <summary>
    /// 丢失的锁 key
    /// </summary>
    public string LockKey { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="lockKey">丢失的锁 key</param>
    /// <param name="message">异常消息</param>
    public DistributedLockLostException(string lockKey, string message)
        : base(message)
    {
        LockKey = lockKey;
    }
}
