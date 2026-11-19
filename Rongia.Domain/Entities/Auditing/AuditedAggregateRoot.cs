using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rongia.Domain.Entities.Auditing;

/// <summary>
/// 支持更新审计信息的聚合根基类:记录更新人、更新人名称、更新时间(对应 Java UpdateAggregateRoot)
/// </summary>
public abstract class AuditedAggregateRoot : CreationAuditedAggregateRoot
{
    /// <summary>
    /// 更新人Id
    /// </summary>
    public long? LastModifierId { get; protected set; }

    /// <summary>
    /// 更新人名称
    /// </summary>
    public string? LastModifier { get; protected set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? LastModificationTime { get; protected set; }

    /// <inheritdoc />
    public void Update(long userId, string userName)
    {
        LastModifierId = userId;
        LastModifier = userName;
        LastModificationTime = DateTime.Now;
    }
}
