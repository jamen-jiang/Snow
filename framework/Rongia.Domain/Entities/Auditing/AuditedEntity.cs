using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rongia.Domain.Entities.Auditing;

/// <summary>
/// 支持更新审计信息的领域实体基类:记录更新人、更新人名称、更新时间(对应 Java BaseUpdateEntity)
/// </summary>
public abstract class AuditedEntity : CreationAuditedEntity
{
    /// <summary>
    /// 更新人Id(可空)
    /// </summary>
    [Column("LastModifierId")]
    public long? LastModifierId { get; protected set; }

    /// <summary>
    /// 更新人名称(可空,nvarchar(50))
    /// </summary>
    [Column("LastModifier"), MaxLength(50)]
    public string? LastModifier { get; protected set; }

    /// <summary>
    /// 更新时间(可空)
    /// </summary>
    [Column("LastModificationTime")]
    public DateTime? LastModificationTime { get; protected set; }

    /// <summary>
    /// 设置更新审计信息,子类可重写以触发领域事件
    /// </summary>
    public virtual void Update(long userId, string userName)
    {
        LastModifierId = userId;
        LastModifier = userName;
        LastModificationTime = DateTime.Now;
    }
}
