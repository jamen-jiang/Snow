using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snow.Domain.Entities.Auditing;

/// <summary>
/// 支持删除审计信息的聚合根基类:记录是否删除、删除人、删除人名称、删除时间(对应 Java DeleteAggregateRoot)
/// </summary>
public abstract class DeletionAuditedAggregateRoot : AggregateRoot, IDeletionAudited
{
    /// <summary>
    /// 是否已删除
    /// </summary>
    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 删除人Id
    /// </summary>
    [Column("DeleterId")]
    public long? DeleterId { get; set; }

    /// <summary>
    /// 删除人名称
    /// </summary>
    [Column("Deleter"), MaxLength(50)]
    public string? Deleter { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    [Column("DeletionTime")]
    public DateTime? DeletionTime { get; set; }

    /// <summary>
    /// 标记为已删除(逻辑删除),子类可重写以触发领域事件
    /// </summary>
    public virtual void Delete()
    {
        IsDeleted = true;
    }

    /// <summary>
    /// 标记为已删除并记录删除人,子类可重写以触发领域事件
    /// </summary>
    /// <param name="deleterId">删除人Id</param>
    /// <param name="deleter">删除人名称</param>
    public virtual void Delete(long? deleterId, string? deleter)
    {
        Delete();
        DeleterId = deleterId;
        Deleter = deleter;
        DeletionTime = DateTime.Now;
    }
}
