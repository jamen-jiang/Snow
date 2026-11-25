using Snow.Auditing;

namespace Snow.Domain.Entities.Auditing;

/// <summary>
/// 支持软删除的聚合根基类:记录是否删除(对应 Java DeleteAggregateRoot)
/// </summary>
public abstract class DeletionAuditedAggregateRoot : AggregateRoot, ISoftDelete
{
    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// 标记为已删除(逻辑删除),子类可重写以触发领域事件
    /// </summary>
    public virtual void Delete()
    {
        IsDeleted = true;
    }
}
