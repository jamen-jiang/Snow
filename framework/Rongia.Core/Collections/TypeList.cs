using System.Collections;

namespace Rongia.Collections;

/// <summary>
/// <see cref="ITypeList{TBase}"/> 的默认实现: 内部就是一个 <see cref="List{T}"/>,
/// 写入时校验元素可赋值给 <typeparamref name="TBase"/>
/// </summary>
/// <typeparam name="TBase">元素允许的基类型</typeparam>
public class TypeList<TBase> : ITypeList<TBase> where TBase : class
{
    private readonly List<Type> _items = new();

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public Type this[int index]
    {
        get => _items[index];
        set
        {
            CheckType(value);
            _items[index] = value;
        }
    }

    /// <inheritdoc />
    public void Add(Type item)
    {
        CheckType(item);
        _items.Add(item);
    }

    /// <inheritdoc />
    public void Add<T>() where T : TBase => _items.Add(typeof(T));

    /// <inheritdoc />
    public bool TryAdd<T>() where T : TBase
    {
        if (Contains<T>())
        {
            return false;
        }

        Add<T>();

        return true;
    }

    /// <inheritdoc />
    public void Clear() => _items.Clear();

    /// <inheritdoc />
    public bool Contains(Type item) => _items.Contains(item);

    /// <inheritdoc />
    public bool Contains<T>() where T : TBase => _items.Contains(typeof(T));

    /// <inheritdoc />
    public void CopyTo(Type[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<Type> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc />
    public int IndexOf(Type item) => _items.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, Type item)
    {
        CheckType(item);
        _items.Insert(index, item);
    }

    /// <inheritdoc />
    public bool Remove(Type item) => _items.Remove(item);

    /// <inheritdoc />
    public void Remove<T>() where T : TBase => _items.Remove(typeof(T));

    /// <inheritdoc />
    public void RemoveAt(int index) => _items.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// 校验类型归属
    /// </summary>
    /// <param name="item">待校验的类型</param>
    /// <exception cref="ArgumentException">类型不能赋值给基类型</exception>
    private static void CheckType(Type item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!typeof(TBase).IsAssignableFrom(item))
        {
            throw new ArgumentException($"类型 {item.FullName} 不能赋值给 {typeof(TBase).FullName}", nameof(item));
        }
    }
}
