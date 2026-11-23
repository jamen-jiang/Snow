namespace Snow.DistributedLocking;

/// <summary>
/// 当前异步调用链上一次加锁的节点: 不可变单链, 只追加不修改(释放时打标记)。
/// <para>身份由「锁实例 + 锁键」确定, 因此节点可以在锁内部状态(entry)存在之前就创建 ——
/// Redis 实现必须等 <c>SET NX</c> 成功才有 entry, 但重入判定又要用到链上的节点。</para>
/// </summary>
internal sealed class DistributedLockFlowNode
{
    /// <summary>
    /// 持有者: 锁实现的实例, 用来区分不同实现持有的同名锁
    /// </summary>
    internal object Owner { get; init; } = null!;

    /// <summary>
    /// 已规范化的锁键
    /// </summary>
    internal string Key { get; init; } = string.Empty;

    /// <summary>
    /// 上一层节点(同一条调用链上更早的加锁)
    /// </summary>
    internal DistributedLockFlowNode? Parent { get; init; }

    /// <summary>
    /// 是否已释放; 释放后不再参与重入判定, 但仍留在链上供外层回溯
    /// </summary>
    internal bool Released { get; set; }
}

/// <summary>
/// 异步调用链上的加锁记录(对应 Redisson 按线程 id 记重入, 这里按 AsyncLocal 的调用链记)。
/// <para>为什么能区分「顺序嵌套」与「并行分支」: 节点在<strong>同步段</strong>写入 AsyncLocal,
/// 所以调用方马上能看到; <c>Task.Run</c> 起的分支拿到的是链的快照, 它看到的最内层节点是父的节点,
/// 而锁的持有栈顶已被先到的分支改写, 两者对不上, 于是并行分支不会被误判成重入。</para>
/// </summary>
internal static class DistributedLockFlow
{
    private static readonly AsyncLocal<DistributedLockFlowNode?> CurrentNode = new();

    /// <summary>
    /// 沿当前链找最近一个「未释放且属于该锁实例与该键」的节点
    /// </summary>
    /// <param name="owner">锁实现实例</param>
    /// <param name="key">已规范化的锁键</param>
    /// <returns>命中的节点; 没持有过返回 null</returns>
    internal static DistributedLockFlowNode? FindHeld(object owner, string key)
    {
        for (DistributedLockFlowNode? node = CurrentNode.Value; node is not null; node = node.Parent)
        {
            if (!node.Released && ReferenceEquals(node.Owner, owner) && string.Equals(node.Key, key, StringComparison.Ordinal))
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>
    /// 在当前链上压入一个新节点(必须在同步段调用, 否则写入只留在异步方法自己的上下文副本里)
    /// </summary>
    /// <param name="owner">锁实现实例</param>
    /// <param name="key">已规范化的锁键</param>
    /// <returns>新节点</returns>
    internal static DistributedLockFlowNode Begin(object owner, string key)
    {
        DistributedLockFlowNode node = new() { Owner = owner, Key = key, Parent = FindLive(CurrentNode.Value) };

        CurrentNode.Value = node;

        return node;
    }

    /// <summary>
    /// 释放一个节点: 打标记; 若它正是当前链的链头, 把链头回退到最近的未释放祖先
    /// </summary>
    /// <param name="node">要释放的节点</param>
    internal static void Release(DistributedLockFlowNode node)
    {
        node.Released = true;

        if (ReferenceEquals(CurrentNode.Value, node))
        {
            CurrentNode.Value = FindLive(node.Parent);
        }
    }

    /// <summary>
    /// 当前链长度(未释放节点数), 用于排查重入记录是否泄漏
    /// </summary>
    /// <returns>未释放节点数</returns>
    internal static int ChainLength()
    {
        int length = 0;

        for (DistributedLockFlowNode? node = FindLive(CurrentNode.Value); node is not null; node = FindLive(node.Parent))
        {
            length++;
        }

        return length;
    }

    /// <summary>
    /// 跳过已释放的节点, 找到最近的未释放祖先
    /// </summary>
    /// <param name="node">起点</param>
    /// <returns>最近的未释放节点; 没有返回 null</returns>
    private static DistributedLockFlowNode? FindLive(DistributedLockFlowNode? node)
    {
        while (node is not null && node.Released)
        {
            node = node.Parent;
        }

        return node;
    }
}
