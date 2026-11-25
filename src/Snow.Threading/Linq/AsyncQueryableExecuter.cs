using System.Linq.Expressions;
using Snow.DependencyInjection;

namespace Snow.Linq;

/// <summary>
/// 异步查询执行器(对应 ABP 的 <c>AsyncQueryableExecuter</c>):
/// 每次执行先问一遍已登记的提供者, 第一个声明能执行的就交给它; 都不行时退回同步 LINQ。
/// </summary>
[SingletonService]
public class AsyncQueryableExecuter : IAsyncQueryableExecuter
{
    /// <summary>
    /// 已登记的异步查询提供者
    /// </summary>
    protected IEnumerable<IAsyncQueryableProvider> Providers { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="providers">已登记的异步查询提供者</param>
    public AsyncQueryableExecuter(IEnumerable<IAsyncQueryableProvider> providers)
    {
        Providers = providers;
    }

    /// <summary>
    /// 找出第一个能异步执行该查询的提供者
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <returns>能执行的提供者, 没有时返回 null</returns>
    protected virtual IAsyncQueryableProvider? FindProvider<T>(IQueryable<T> queryable)
    {
        return Providers.FirstOrDefault(provider => provider.CanExecute(queryable));
    }

    /// <summary>
    /// 判断查询结果里是否包含指定元素
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="item">要查找的元素</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<bool> ContainsAsync<T>(IQueryable<T> queryable, T item, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.ContainsAsync(queryable, item, cancellationToken);
        }

        return Task.FromResult(queryable.Contains(item));
    }

    /// <summary>
    /// 判断查询结果是否有任意元素
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<bool> AnyAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AnyAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Any());
    }

    /// <summary>
    /// 判断查询结果里是否有满足条件的元素
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<bool> AnyAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AnyAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.Any(predicate));
    }

    /// <summary>
    /// 判断查询结果里是否所有元素都满足条件
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<bool> AllAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AllAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.All(predicate));
    }

    /// <summary>
    /// 统计查询结果的元素个数
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.CountAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Count());
    }

    /// <summary>
    /// 统计满足条件的元素个数
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<int> CountAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.CountAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.Count(predicate));
    }

    /// <summary>
    /// 统计查询结果的元素个数(64 位)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<long> LongCountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.LongCountAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.LongCount());
    }

    /// <summary>
    /// 统计满足条件的元素个数(64 位)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<long> LongCountAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.LongCountAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.LongCount(predicate));
    }

    /// <summary>
    /// 取第一个元素, 没有元素时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T> FirstAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.FirstAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.First());
    }

    /// <summary>
    /// 取第一个满足条件的元素, 没有时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T> FirstAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.FirstAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.First(predicate));
    }

    /// <summary>
    /// 取第一个元素, 没有元素时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.FirstOrDefaultAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.FirstOrDefault())!;
    }

    /// <summary>
    /// 取第一个满足条件的元素, 没有时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.FirstOrDefaultAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.FirstOrDefault(predicate))!;
    }

    /// <summary>
    /// 取最后一个元素, 没有元素时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T> LastAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.LastAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Last());
    }

    /// <summary>
    /// 取最后一个满足条件的元素, 没有时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T> LastAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.LastAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.Last(predicate));
    }

    /// <summary>
    /// 取最后一个元素, 没有元素时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T?> LastOrDefaultAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.LastOrDefaultAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.LastOrDefault())!;
    }

    /// <summary>
    /// 取最后一个满足条件的元素, 没有时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T?> LastOrDefaultAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.LastOrDefaultAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.LastOrDefault(predicate))!;
    }

    /// <summary>
    /// 取唯一元素, 元素个数不为 1 时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T> SingleAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SingleAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Single());
    }

    /// <summary>
    /// 取唯一满足条件的元素, 个数不为 1 时抛异常
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T> SingleAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SingleAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.Single(predicate));
    }

    /// <summary>
    /// 取唯一元素, 没有元素时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SingleOrDefaultAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.SingleOrDefault())!;
    }

    /// <summary>
    /// 取唯一满足条件的元素, 没有时返回默认值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="predicate">过滤条件</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SingleOrDefaultAsync(queryable, predicate, cancellationToken);
        }

        return Task.FromResult(queryable.SingleOrDefault(predicate))!;
    }

    /// <summary>
    /// 取最小值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T> MinAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.MinAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Min())!;
    }

    /// <summary>
    /// 按投影取最小值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <typeparam name="TResult">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<TResult> MinAsync<T, TResult>(IQueryable<T> queryable, Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.MinAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Min(selector))!;
    }

    /// <summary>
    /// 取最大值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T> MaxAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.MaxAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Max())!;
    }

    /// <summary>
    /// 按投影取最大值
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <typeparam name="TResult">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<TResult> MaxAsync<T, TResult>(IQueryable<T> queryable, Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.MaxAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Max(selector))!;
    }

    /// <summary>
    /// 求和(decimal)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<decimal> SumAsync(IQueryable<decimal> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 求和(decimal?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<decimal?> SumAsync(IQueryable<decimal?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 求和(int)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<int> SumAsync(IQueryable<int> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 求和(int?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<int?> SumAsync(IQueryable<int?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 求和(long)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<long> SumAsync(IQueryable<long> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 求和(long?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<long?> SumAsync(IQueryable<long?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 求和(double)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double> SumAsync(IQueryable<double> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 求和(double?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double?> SumAsync(IQueryable<double?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 求和(float)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<float> SumAsync(IQueryable<float> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 求和(float?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<float?> SumAsync(IQueryable<float?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Sum());
    }

    /// <summary>
    /// 按投影求和(decimal)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<decimal> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 按投影求和(decimal?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<decimal?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, decimal?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 按投影求和(int)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<int> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 按投影求和(int?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<int?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, int?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 按投影求和(long)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<long> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, long>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 按投影求和(long?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<long?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, long?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 按投影求和(double)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, double>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 按投影求和(double?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, double?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 按投影求和(float)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<float> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, float>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 按投影求和(float?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<float?> SumAsync<T>(IQueryable<T> queryable, Expression<Func<T, float?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.SumAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Sum(selector));
    }

    /// <summary>
    /// 求平均值(decimal)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<decimal> AverageAsync(IQueryable<decimal> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 求平均值(decimal?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<decimal?> AverageAsync(IQueryable<decimal?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 求平均值(int)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double> AverageAsync(IQueryable<int> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 求平均值(int?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double?> AverageAsync(IQueryable<int?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 求平均值(long)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double> AverageAsync(IQueryable<long> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 求平均值(long?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double?> AverageAsync(IQueryable<long?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 求平均值(double)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double> AverageAsync(IQueryable<double> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 求平均值(double?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double?> AverageAsync(IQueryable<double?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 求平均值(float)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<float> AverageAsync(IQueryable<float> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 求平均值(float?)
    /// </summary>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<float?> AverageAsync(IQueryable<float?> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.Average());
    }

    /// <summary>
    /// 按投影求平均值(decimal)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<decimal> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 按投影求平均值(decimal?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<decimal?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, decimal?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 按投影求平均值(int)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 按投影求平均值(int?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, int?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 按投影求平均值(long)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, long>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 按投影求平均值(long?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, long?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 按投影求平均值(double)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, double>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 按投影求平均值(double?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<double?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, double?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 按投影求平均值(float)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<float> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, float>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 按投影求平均值(float?)
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="selector">投影选择器</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<float?> AverageAsync<T>(IQueryable<T> queryable, Expression<Func<T, float?>> selector, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.AverageAsync(queryable, selector, cancellationToken);
        }

        return Task.FromResult(queryable.Average(selector));
    }

    /// <summary>
    /// 把查询结果取成列表
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.ToListAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.ToList());
    }

    /// <summary>
    /// 把查询结果取成数组
    /// </summary>
    /// <typeparam name="T">类型参数</typeparam>
    /// <param name="queryable">待执行的查询</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual Task<T[]> ToArrayAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        IAsyncQueryableProvider? provider = FindProvider(queryable);

        if (provider != null)
        {
            return provider.ToArrayAsync(queryable, cancellationToken);
        }

        return Task.FromResult(queryable.ToArray());
    }
}
