using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snow.Domain.Entities.Auditing;

/// <summary>
/// 支持创建审计信息的领域实体基类:记录创建人、创建人名称、创建时间(对应 Java BaseCreateEntity)
/// </summary>
public abstract class CreationAuditedEntity : DeletionAuditedEntity, ICreationAudited
{
    /// <summary>
    /// 创建人Id
    /// </summary>
    [Column("CreatorId")]
    public long CreatorId { get; set; }

    /// <summary>
    /// 创建人名称
    /// </summary>
    [Column("Creator"), MaxLength(50)]
    public string Creator { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column("CreationTime")]
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 设置创建审计信息,子类可重写以触发领域事件;dateTime 为空时取当前时间
    /// </summary>
    /// <param name="userId">创建人Id</param>
    /// <param name="userName">创建人名称</param>
    /// <param name="dateTime">创建时间</param>
    public virtual void Create(long userId, string userName, DateTime? dateTime = null)
    {
        CreatorId = userId;
        Creator = userName;
        CreationTime = dateTime ?? DateTime.Now;
    }
}
