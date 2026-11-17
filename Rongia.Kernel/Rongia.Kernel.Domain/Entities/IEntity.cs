namespace Rongia.Kernel.Domain.Entities;

/// <summary>
/// 实体接口(对应 Java com.rongia.kernel.domain.entity.Entity)
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 主键,未持久化时为 0(对应 Java getId() 的 0 出口约定)
    /// </summary>
    long Id { get; }
}
