using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rongia.Domain.Entities.Auditing;

/// <summary>
/// 支持创建审计信息的领域实体基类:记录创建人、创建人名称、创建时间(对应 Java BaseCreateEntity)
/// </summary>
public abstract class CreationAuditedEntity : DeletionAuditedEntity
{
    /// <summary>
    /// 创建人Id
    /// </summary>
    [Column("CreatorId")]
    public long CreatorId { get; protected set; }

    /// <summary>
    /// 创建人名称
    /// </summary>
    [Column("Creator"), MaxLength(50)]
    public string Creator { get; protected set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column("CreationTime")]
    public DateTime CreationTime { get; protected set; }

    /// <summary>
    /// 设置创建审计信息,子类可重写以触发领域事件;dateTime 为空时取当前时间
    /// </summary>
    public virtual void Create(long userId, string userName, DateTime? dateTime = null)
    {
        CreatorId = userId;
        Creator = userName;
        CreationTime = dateTime ?? DateTime.Now;
    }
}
