namespace Rongia.Domain.Entities;

/// <summary>
/// 支持更新审计信息的实体接口:记录更新人、更新人名称、更新时间(对应 Java UpdateEntity)
/// </summary>
public interface IUpdateEntity
{
    /// <summary>
    /// 更新人Id
    /// </summary>
    long? UpdatedBy { get; }

    /// <summary>
    /// 更新人名称
    /// </summary>
    string? UpdatedByName { get; }

    /// <summary>
    /// 更新时间
    /// </summary>
    DateTime? UpdatedTime { get; }

    /// <summary>
    /// 设置更新审计信息,子类可重写以触发领域事件
    /// </summary>
    void Update(long userId, string userName);
}
