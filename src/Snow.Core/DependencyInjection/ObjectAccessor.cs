namespace Snow.DependencyInjection;

/// <summary>
/// 对象访问器的默认实现: 只是一个可读写的字段, 由宿主在构建完成后填值
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class ObjectAccessor<T> : IObjectAccessor<T>
{
    /// <inheritdoc />
    public T? Value { get; set; }

    /// <summary>
    /// 创建一个空访问器, 由宿主在容器构建完成后填值
    /// </summary>
    public ObjectAccessor()
    {
    }

    /// <summary>
    /// 用给定初始值创建访问器(对应 ABP 的 ObjectAccessor&lt;T&gt;(T value) 构造)
    /// </summary>
    /// <param name="value">初始值</param>
    public ObjectAccessor(T? value)
    {
        Value = value;
    }
}
