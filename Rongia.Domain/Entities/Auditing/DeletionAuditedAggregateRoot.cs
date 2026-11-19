using System.ComponentModel.DataAnnotations.Schema;

namespace Rongia.Domain.Entities.Auditing;

/// <summary>
/// 支持更新审计信息的领域实体基类:记录更新人、更新人名称、更新时间(对应 Java BaseUpdateEntity)
/// </summary>
public abstract class DeletionAuditedAggregateRoot : AggregateRoot
{
    /// <summary>
    /// 是否已删除
    /// </summary>
    [Column("IsDeleted")]
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// 标记为已删除(逻辑删除),子类可重写以触发领域事件
    /// </summary>
    public virtual void Delete()
    {
        IsDeleted = true;
    }
}
