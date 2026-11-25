using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Snow;
using Snow.Auditing;

namespace Snow.Domain.Entities.Auditing;

/// <summary>
/// 支持更新审计信息的聚合根基类:记录更新人、更新人名称、更新时间(对应 Java UpdateAggregateRoot)
/// </summary>
public abstract class AuditedAggregateRoot : CreationAuditedAggregateRoot, IModificationAudited
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

    /// <summary>
    /// 设置更新审计信息
    /// </summary>
    /// <param name="userId">更新人Id</param>
    /// <param name="userName">更新人名称</param>
    public virtual void Update(long userId, string userName)
    {
        LastModifierId = userId;
        LastModifier = userName;
        LastModificationTime = DateTime.Now;
    }
}
