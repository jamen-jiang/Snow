using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Messaging.Wrappers;

/// <summary>
/// 查询的包装器:解析 <see cref="IQueryHandler{TQuery,TResponse}"/> 并按注册顺序套上管道行为。
/// 闭式泛型由 <see cref="QueryBus"/> 在运行时按查询类型构造,并用缓存的委托调用,避免每次查询都做反射。
/// </summary>
/// <typeparam name="TQuery">查询类型</typeparam>
/// <typeparam name="TResponse">返回值类型</typeparam>
public class QueryHandlerWrapper<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// 执行查询
    /// </summary>
    /// <param name="query">查询实例</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果</returns>
    public Task<TResponse> Handle(IQuery<TResponse> query, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Task<TResponse> Handler(CancellationToken t = default) => serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>()
            .HandleAsync((TQuery) query, t == default ? cancellationToken : t);

        return serviceProvider
            .GetServices<IPipelineBehavior<TQuery, TResponse>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate<TResponse>) Handler,
                (next, pipeline) => (t) => pipeline.HandleAsync((TQuery) query, next, t == default ? cancellationToken : t))();
    }
}
