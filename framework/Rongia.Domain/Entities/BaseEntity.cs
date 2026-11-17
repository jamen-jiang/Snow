using System.Runtime.CompilerServices;

namespace Rongia.Domain.Entities;

/// <summary>
/// 实体抽象基类(对应 Java BaseEntity)
/// </summary>
/// <remarks>
/// 主键由数据库自增生成(SQL Server IDENTITY,对应 Java 的 @Id @GeneratedValue(IDENTITY)),
/// 未持久化时 Id 为 0。列映射由 Infrastructure 层的 EF Core 按约定识别。
/// </remarks>
public abstract class BaseEntity : IEntity
{
    /// <summary>
    /// 主键,未持久化时为 0,由数据库自增回填
    /// </summary>
    public long Id { get; protected set; }

    /// <summary>
    /// 相等性判断:仅当双方都已持久化且 Id 相等时视为同一实体。
    /// 任一方未持久化(Id 为 0)时只有引用相同才相等。
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj is not BaseEntity entity)
        {
            return false;
        }
        if (Id == 0L || entity.Id == 0L)
        {
            return false;
        }
        return Id == entity.Id;
    }

    /// <summary>
    /// 哈希值:未持久化时用对象引用,避免所有新对象哈希冲突
    /// </summary>
    public override int GetHashCode()
    {
        return Id == 0L ? RuntimeHelpers.GetHashCode(this) : Id.GetHashCode();
    }

    /// <summary>
    /// 字符串表示:类名 + Id
    /// </summary>
    public override string ToString()
    {
        return string.Create(null, stackalloc char[128], $"Entity of type {GetType().FullName} with id: {Id}");
    }
}
