namespace Snow.Domain.Entities;

/// <summary>
/// 实体不存在异常(对齐 ABP 的 <c>EntityNotFoundException</c>)
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// 查询使用的主键
    /// </summary>
    public object? Id { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="id">主键</param>
    public EntityNotFoundException(Type entityType, object? id)
        : base($"找不到实体 {entityType.FullName}, 主键: {id}")
    {
        EntityType = entityType;
        Id = id;
    }
}
