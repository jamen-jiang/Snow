using System.Linq.Expressions;
using Snow;

namespace System.Linq;

/// <summary>
/// 针对 <see cref="IQueryable{T}"/> 的扩展方法。
/// </summary>
public static class SnowQueryableExtensions
{
    /// <summary>
    /// 分页取数: 等价于 Skip(skipCount).Take(maxResultCount) 的简写。
    /// </summary>
    public static IQueryable<T> PageBy<T>(this IQueryable<T> query, int skipCount, int maxResultCount)
    {
        Check.NotNull(query, nameof(query));

        return query.Skip(skipCount).Take(maxResultCount);
    }

    /// <summary>
    /// 分页取数: 等价于 Skip(skipCount).Take(maxResultCount) 的简写, 并保留调用方的查询类型。
    /// </summary>
    public static TQueryable PageBy<T, TQueryable>(this TQueryable query, int skipCount, int maxResultCount)
        where TQueryable : IQueryable<T>
    {
        Check.NotNull(query, nameof(query));

        return (TQueryable)query.Skip(skipCount).Take(maxResultCount);
    }

    /// <summary>
    /// 只在 <paramref name="condition"/> 为 true 时按条件过滤, 否则原样返回。
    /// </summary>
    /// <param name="query">要过滤的查询</param>
    /// <param name="condition">是否执行过滤</param>
    /// <param name="predicate">过滤条件</param>
    /// <returns>条件为 true 时返回过滤后的查询, 否则返回原查询</returns>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
    {
        Check.NotNull(query, nameof(query));

        return condition
            ? query.Where(predicate)
            : query;
    }

    /// <summary>
    /// 只在 <paramref name="condition"/> 为 true 时按条件过滤, 否则原样返回, 并保留调用方的查询类型。
    /// </summary>
    /// <param name="query">要过滤的查询</param>
    /// <param name="condition">是否执行过滤</param>
    /// <param name="predicate">过滤条件</param>
    /// <returns>条件为 true 时返回过滤后的查询, 否则返回原查询</returns>
    public static TQueryable WhereIf<T, TQueryable>(this TQueryable query, bool condition, Expression<Func<T, bool>> predicate)
        where TQueryable : IQueryable<T>
    {
        Check.NotNull(query, nameof(query));

        return condition
            ? (TQueryable)query.Where(predicate)
            : query;
    }

    /// <summary>
    /// 只在 <paramref name="condition"/> 为 true 时按条件过滤, 否则原样返回。
    /// </summary>
    /// <param name="query">要过滤的查询</param>
    /// <param name="condition">是否执行过滤</param>
    /// <param name="predicate">带元素下标的过滤条件</param>
    /// <returns>条件为 true 时返回过滤后的查询, 否则返回原查询</returns>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, int, bool>> predicate)
    {
        Check.NotNull(query, nameof(query));

        return condition
            ? query.Where(predicate)
            : query;
    }

    /// <summary>
    /// 只在 <paramref name="condition"/> 为 true 时按条件过滤, 否则原样返回, 并保留调用方的查询类型。
    /// </summary>
    /// <param name="query">要过滤的查询</param>
    /// <param name="condition">是否执行过滤</param>
    /// <param name="predicate">带元素下标的过滤条件</param>
    /// <returns>条件为 true 时返回过滤后的查询, 否则返回原查询</returns>
    public static TQueryable WhereIf<T, TQueryable>(this TQueryable query, bool condition, Expression<Func<T, int, bool>> predicate)
        where TQueryable : IQueryable<T>
    {
        Check.NotNull(query, nameof(query));

        return condition
            ? (TQueryable)query.Where(predicate)
            : query;
    }

    /// <summary>
    /// 只在 <paramref name="condition"/> 为 true 时排序, 否则原样返回。
    /// </summary>
    /// <param name="query">要排序的查询</param>
    /// <param name="condition">是否执行排序</param>
    /// <param name="sorting">动态排序串, 如 "Name desc, Id"</param>
    /// <returns>条件为 true 时返回排序后的查询, 否则返回原查询</returns>
    public static TQueryable OrderByIf<T, TQueryable>(this TQueryable query, bool condition, string sorting)
        where TQueryable : IQueryable<T>
    {
        Check.NotNull(query, nameof(query));

        return condition
            ? (TQueryable)global::System.Linq.Dynamic.Core.DynamicQueryableExtensions.OrderBy(query, sorting)
            : query;
    }
}
