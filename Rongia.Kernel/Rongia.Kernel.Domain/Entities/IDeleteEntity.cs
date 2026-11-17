namespace Rongia.Kernel.Domain.Entities;

/// <summary>
/// 支持逻辑删除(软删除)的实体接口,适用于需要"假删除"功能的业务实体(对应 Java DeleteEntity)
/// </summary>
public interface IDeleteEntity
{
    /// <summary>
    /// 是否已删除
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// 标记为已删除(逻辑删除),子类可重写以触发领域事件
    /// </summary>
    void Delete();
}
