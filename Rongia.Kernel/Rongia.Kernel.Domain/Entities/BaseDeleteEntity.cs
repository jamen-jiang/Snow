using System.ComponentModel.DataAnnotations.Schema;

namespace Rongia.Kernel.Domain.Entities;

/// <summary>
/// 支持逻辑删除(软删除)的实体基类(对应 Java BaseDeleteEntity)
/// </summary>
/// <remarks>
/// 软删除的查询过滤与删除 SQL(对应 Java 的 @SQLRestriction / @SQLDelete)
/// 由 Infrastructure 层的 EF Core 全局查询过滤器与 SaveChanges 拦截器实现。
/// </remarks>
public abstract class BaseDeleteEntity : BaseEntity, IDeleteEntity
{
    /// <summary>
    /// 逻辑删除标识 0 = 未删除,1 = 已删除
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
