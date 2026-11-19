namespace Rongia.Authorization;

/// <summary>
/// 类型列表: 支持以泛型方式登记类型(对应 ABP 的 <c>ITypeList</c>)
/// </summary>
public sealed class TypeList : List<Type>
{
    /// <summary>
    /// 登记一个类型
    /// </summary>
    public void Add<T>() => Add(typeof(T));

    /// <summary>
    /// 是否已登记某类型
    /// </summary>
    public bool Contains<T>() => base.Contains(typeof(T));

    /// <summary>
    /// 移除某类型
    /// </summary>
    public void Remove<T>() => base.Remove(typeof(T));
}
