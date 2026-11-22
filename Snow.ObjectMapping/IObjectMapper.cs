namespace Snow.ObjectMapping;

/// <summary>
/// 对象映射器(对应 ABP 的 <c>IObjectMapper</c>): 按目标类型把源对象转成新对象, 或写进已有对象
/// </summary>
public interface IObjectMapper
{
    /// <summary>
    /// 映射成指定类型的新对象
    /// </summary>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <returns>目标对象, 源为 null 时返回默认值</returns>
    TDestination? Map<TDestination>(object? source);

    /// <summary>
    /// 映射成指定类型的新对象, 源类型显式声明
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <returns>目标对象</returns>
    TDestination? Map<TSource, TDestination>(TSource? source);

    /// <summary>
    /// 把源对象映射进已有的目标对象(就地填充, 不新建实例)
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="destination">已有目标对象</param>
    /// <returns>填好的目标对象</returns>
    TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination);

    /// <summary>
    /// 按运行时类型映射, 供非泛型场景使用
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="destinationType">目标类型</param>
    /// <returns>目标对象</returns>
    object? Map(object? source, Type sourceType, Type destinationType);

    /// <summary>
    /// 按运行时类型映射进已有对象, 供非泛型场景使用
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="destination">已有目标对象</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="destinationType">目标类型</param>
    /// <returns>填好的目标对象</returns>
    object? Map(object? source, object? destination, Type sourceType, Type destinationType);
}
