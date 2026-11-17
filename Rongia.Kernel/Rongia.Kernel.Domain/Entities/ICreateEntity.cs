namespace Rongia.Kernel.Domain.Entities;

/// <summary>
/// 支持创建审计信息的实体接口:记录创建人、创建人名称、创建时间(对应 Java CreateEntity)
/// </summary>
public interface ICreateEntity
{
    /// <summary>
    /// 创建人Id
    /// </summary>
    long CreatedBy { get; }

    /// <summary>
    /// 创建人名称
    /// </summary>
    string CreatedByName { get; }

    /// <summary>
    /// 创建时间
    /// </summary>
    DateTime CreatedTime { get; }

    /// <summary>
    /// 设置创建审计信息,子类可重写以触发领域事件;dateTime 为空时取当前时间
    /// </summary>
    void Create(long userId, string userName, DateTime? dateTime = null);
}
