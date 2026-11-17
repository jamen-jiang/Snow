using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rongia.Domain.Entities;

/// <summary>
/// 支持创建审计信息的领域实体基类:记录创建人、创建人名称、创建时间(对应 Java BaseCreateEntity)
/// </summary>
public abstract class BaseCreateEntity : BaseDeleteEntity, ICreateEntity
{
    /// <summary>
    /// 创建人Id(必填)
    /// </summary>
    [Column("CreatedBy")]
    public long CreatedBy { get; protected set; }

    /// <summary>
    /// 创建人名称(必填,nvarchar(50))
    /// </summary>
    [Column("CreatedByName"), MaxLength(50)]
    public string CreatedByName { get; protected set; } = string.Empty;

    /// <summary>
    /// 创建时间(必填)
    /// </summary>
    [Column("CreatedTime")]
    public DateTime CreatedTime { get; protected set; }

    /// <summary>
    /// 设置创建审计信息,子类可重写以触发领域事件;dateTime 为空时取当前时间
    /// </summary>
    public virtual void Create(long userId, string userName, DateTime? dateTime = null)
    {
        CreatedBy = userId;
        CreatedByName = userName;
        CreatedTime = dateTime ?? DateTime.Now;
    }
}
