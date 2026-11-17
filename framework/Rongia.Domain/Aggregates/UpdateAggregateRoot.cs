using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rongia.Domain.Entities;

namespace Rongia.Domain.Aggregates;

/// <summary>
/// 支持更新审计信息的聚合根基类:记录更新人、更新人名称、更新时间(对应 Java UpdateAggregateRoot)
/// </summary>
public abstract class UpdateAggregateRoot : CreateAggregateRoot, IUpdateEntity
{
    /// <summary>
    /// 更新人Id(可空)
    /// </summary>
    [Column("UpdatedBy")]
    public long? UpdatedBy { get; protected set; }

    /// <summary>
    /// 更新人名称(可空,nvarchar(50))
    /// </summary>
    [Column("UpdatedByName"), MaxLength(50)]
    public string? UpdatedByName { get; protected set; }

    /// <summary>
    /// 更新时间(可空)
    /// </summary>
    [Column("UpdatedTime")]
    public DateTime? UpdatedTime { get; protected set; }

    /// <inheritdoc />
    public void Update(long userId, string userName)
    {
        UpdatedBy = userId;
        UpdatedByName = userName;
        UpdatedTime = DateTime.Now;
    }
}
