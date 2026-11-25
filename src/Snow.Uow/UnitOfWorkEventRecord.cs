namespace Snow.Uow;

/// <summary>
/// 工作单元事件记录: 事件类型、事件数据与全局递增序号
/// <para>序号由 <see cref="EventOrderGenerator"/> 发放, 派发时按它排序, 保证事件顺序与产生顺序一致。</para>
/// </summary>
public sealed class UnitOfWorkEventRecord
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件实例</param>
    /// <param name="eventOrder">全局递增序号</param>
    public UnitOfWorkEventRecord(Type eventType, object eventData, long eventOrder)
    {
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        EventData = eventData ?? throw new ArgumentNullException(nameof(eventData));
        EventOrder = eventOrder;
    }

    /// <summary>
    /// 事件类型
    /// </summary>
    public Type EventType { get; }

    /// <summary>
    /// 事件实例
    /// </summary>
    public object EventData { get; }

    /// <summary>
    /// 全局递增序号
    /// </summary>
    public long EventOrder { get; }
}
