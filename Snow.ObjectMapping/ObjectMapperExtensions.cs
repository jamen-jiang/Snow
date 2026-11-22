namespace Snow.ObjectMapping;

/// <summary>
/// <see cref="IObjectMapper"/> 的链式扩展: 让调用处写成 <c>source.Map&lt;OrderDto&gt;()</c>
/// </summary>
public static class ObjectMapperExtensions
{
    /// <summary>
    /// 映射成指定类型的新对象
    /// </summary>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="mapper">映射器</param>
    /// <param name="source">源对象</param>
    /// <returns>目标对象</returns>
    public static TDestination? Map<TDestination>(this IObjectMapper mapper, object? source)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return mapper.Map<TDestination>(source);
    }

    /// <summary>
    /// 映射成指定类型的新对象, 源类型显式声明
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="mapper">映射器</param>
    /// <param name="source">源对象</param>
    /// <returns>目标对象</returns>
    public static TDestination? Map<TSource, TDestination>(this IObjectMapper mapper, TSource? source)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return mapper.Map<TSource, TDestination>(source);
    }

    /// <summary>
    /// 把源对象映射进已有的目标对象
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="mapper">映射器</param>
    /// <param name="source">源对象</param>
    /// <param name="destination">已有目标对象</param>
    /// <returns>填好的目标对象</returns>
    public static TDestination? Map<TSource, TDestination>(this IObjectMapper mapper, TSource? source, TDestination? destination)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return mapper.Map(source, destination);
    }

    /// <summary>
    /// 按运行时类型映射, 供非泛型场景使用
    /// </summary>
    /// <param name="mapper">映射器</param>
    /// <param name="source">源对象</param>
    /// <param name="destinationType">目标类型</param>
    /// <returns>目标对象</returns>
    public static object? Map(this IObjectMapper mapper, object? source, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(destinationType);

        return mapper.Map(source, source?.GetType() ?? destinationType, destinationType);
    }
}
