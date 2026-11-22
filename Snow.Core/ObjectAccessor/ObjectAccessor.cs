namespace Snow;

/// <summary>
/// 对象访问器的默认实现: 只是一个可读写的字段, 由宿主在构建完成后填值
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public class ObjectAccessor<T> : IObjectAccessor<T>
{
    /// <inheritdoc />
    public T? Value { get; set; }
}
