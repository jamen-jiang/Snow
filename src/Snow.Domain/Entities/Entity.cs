using System.ComponentModel.DataAnnotations.Schema;

namespace Snow.Domain.Entities;

/// <summary>
/// 实体基类:以 <see cref="long"/> 型 <see cref="Id"/> 作为唯一标识
/// </summary>
[Serializable]
public abstract class Entity : IEntity
{
    /// <inheritdoc/>
    public long Id { get; private set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Id = {Id}";
    }
}
