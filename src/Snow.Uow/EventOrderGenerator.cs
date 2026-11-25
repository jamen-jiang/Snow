namespace Snow.Uow;

/// <summary>
/// 事件序号生成器: 进程内全局递增, 同一工作单元里的事件据此排序派发
/// </summary>
public static class EventOrderGenerator
{
    private static long _lastOrder;

    /// <summary>
    /// 取下一个序号
    /// </summary>
    /// <returns>递增后的序号</returns>
    public static long GetNext()
    {
        return Interlocked.Increment(ref _lastOrder);
    }
}
