using Snow.Auditing;

namespace Snow.Domain.Entities.Auditing;

/// <summary>
/// 支持软删除的领域实体基类:记录是否删除(对应 Java BaseDeleteEntity)
/// </summary>
public abstract class DeletionAuditedEntity : Entity, ISoftDelete
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
