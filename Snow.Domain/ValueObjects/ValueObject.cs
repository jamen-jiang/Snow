namespace Snow.Domain.ValueObjects;

/// <summary>
/// 值对象抽象基类:通过属性值判等,无唯一标识,通常不可变(对应 Java ValueObject)。
/// <para>子类实现 GetEqualityComponents 返回参与判等的属性序列。</para>
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// 返回参与判等的属性
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// 基于属性值判等
    /// </summary>
    public bool Equals(ValueObject? other)
    {
        if (other is null || GetType() != other.GetType())
        {
            return false;
        }
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    /// <summary>
    /// 基于属性值计算哈希
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            return GetEqualityComponents().Aggregate(17, (hash, component) => hash * 31 + (component?.GetHashCode() ?? 0));
        }
    }
}
