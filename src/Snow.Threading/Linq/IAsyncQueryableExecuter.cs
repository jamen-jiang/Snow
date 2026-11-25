using System.Linq.Expressions;

namespace Snow.Linq;

/// <summary>
/// 异步查询执行器契约(对应 ABP 的 <c>IAsyncQueryableExecuter</c>): 有提供者能异步执行就交给它, 否则退回同步 LINQ 并包成已完成的任务
/// </summary>
public interface IAsyncQueryableExecuter
{
    /// <summary>
    /// 判断查询结果里是否包含指定元素
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="item">要查找的元素</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<bool> ContainsAsync<T>(IQueryable<T> queryable, T item, CancellationToken cancellationToken);

    /// <summary>
    /// 判断查询结果是否有任意元素
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<bool> AnyAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 判断查询结果里是否有满足条件的元素
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<bool> AnyAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 判断查询结果里是否所有元素都满足条件
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<bool> AllAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 统计查询结果的元素个数
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 统计满足条件的元素个数
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<int> CountAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 统计查询结果的元素个数(64 位)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<long> LongCountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 统计满足条件的元素个数(64 位)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<long> LongCountAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 取第一个元素, 没有元素时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T> FirstAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 取第一个满足条件的元素, 没有时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T> FirstAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 取第一个元素, 没有元素时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 取第一个满足条件的元素, 没有时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 取最后一个元素, 没有元素时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T> LastAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 取最后一个满足条件的元素, 没有时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T> LastAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 取最后一个元素, 没有元素时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T?> LastOrDefaultAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 取最后一个满足条件的元素, 没有时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T?> LastOrDefaultAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 取唯一元素, 元素个数不为 1 时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T> SingleAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 取唯一满足条件的元素, 个数不为 1 时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T> SingleAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 取唯一元素, 没有元素时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 取唯一满足条件的元素, 没有时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    /// <summary>
    /// 取最小值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T> MinAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影取最小值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <typeparam name="TResult">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<TResult> MinAsync<T, TResult>(IQueryable<T> queryable, Expression<Func<T, TResult>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 取最大值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T> MaxAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影取最大值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <typeparam name="TResult">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<TResult> MaxAsync<T, TResult>(IQueryable<T> queryable, Expression<Func<T, TResult>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(decimal)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<decimal> SumAsync(IQueryable<decimal> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(decimal?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<decimal?> SumAsync(IQueryable<decimal?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(int)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<int> SumAsync(IQueryable<int> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(int?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<int?> SumAsync(IQueryable<int?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(long)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<long> SumAsync(IQueryable<long> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(long?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<long?> SumAsync(IQueryable<long?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(double)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double> SumAsync(IQueryable<double> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(double?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double?> SumAsync(IQueryable<double?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(float)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<float> SumAsync(IQueryable<float> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求和(float?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<float?> SumAsync(IQueryable<float?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(decimal)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<decimal> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(decimal?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<decimal?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, decimal?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(int)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<int> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, int>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(int?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<int?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, int?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(long)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<long> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, long>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(long?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<long?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, long?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(double)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, double>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(double?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, double?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(float)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<float> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, float>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求和(float?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<float?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, float?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(decimal)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<decimal> AverageAsync(IQueryable<decimal> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(decimal?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<decimal?> AverageAsync(IQueryable<decimal?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(int)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double> AverageAsync(IQueryable<int> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(int?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double?> AverageAsync(IQueryable<int?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(long)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double> AverageAsync(IQueryable<long> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(long?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double?> AverageAsync(IQueryable<long?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(double)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double> AverageAsync(IQueryable<double> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(double?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double?> AverageAsync(IQueryable<double?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(float)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<float> AverageAsync(IQueryable<float> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 求平均值(float?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<float?> AverageAsync(IQueryable<float?> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(decimal)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<decimal> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(decimal?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<decimal?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, decimal?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(int)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, int>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(int?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, int?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(long)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, long>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(long?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, long?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(double)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, double>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(double?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<double?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, double?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(float)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<float> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, float>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 按投影求平均值(float?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<float?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, float?>> selector, CancellationToken cancellationToken);

    /// <summary>
    /// 把查询结果取成列表
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);

    /// <summary>
    /// 把查询结果取成数组
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    Task<T[]> ToArrayAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken);
}
