namespace Snow.Uow;

/// <summary>
/// 工作单元事件派发器: 把攒到工作单元里的事件按顺序交给事件总线
/// </summary>
public interface IUnitOfWorkEventPublisher
{
    /// <summary>
    /// 派发本地事件(进程内事件总线)
    /// </summary>
    /// <param name="localEvents">已按 <see cref="UnitOfWorkEventRecord.EventOrder"/> 排序的事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task PublishLocalEventsAsync(IEnumerable<UnitOfWorkEventRecord> localEvents,
        CancellationToken cancellationToken = default);
}
