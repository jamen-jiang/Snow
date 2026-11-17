using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Wrappers;

public abstract class RequestHandlerBase
{
    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

public abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    public abstract Task<TResponse> Handle(ICommand<TResponse> command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

public abstract class RequestHandlerWrapper : RequestHandlerBase
{
    public abstract Task<Unit> Handle(ICommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

public abstract class QueryRequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    public abstract Task<TResponse> Handle(IQuery<TResponse> query, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

public class RequestHandlerWrapperImpl<TCommand, TResponse> : RequestHandlerWrapper<TResponse>
    where TCommand : ICommand<TResponse>
{
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken) =>
        await Handle((ICommand<TResponse>) request, serviceProvider, cancellationToken).ConfigureAwait(false);

    public override Task<TResponse> Handle(ICommand<TResponse> command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Task<TResponse> Handler(CancellationToken t = default) => serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>()
            .Handle((TCommand) command, t == default ? cancellationToken : t);

        return serviceProvider
            .GetServices<IPipelineBehavior<TCommand, TResponse>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate<TResponse>) Handler,
                (next, pipeline) => (t) => pipeline.Handle((TCommand) command, next, t == default ? cancellationToken : t))();
    }
}

public class RequestHandlerWrapperImpl<TCommand> : RequestHandlerWrapper
    where TCommand : ICommand
{
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken) =>
        await Handle((ICommand) request, serviceProvider, cancellationToken).ConfigureAwait(false);

    public override Task<Unit> Handle(ICommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        async Task<Unit> Handler(CancellationToken t = default)
        {
            await serviceProvider.GetRequiredService<ICommandHandler<TCommand>>()
                .Handle((TCommand) command, t == default ? cancellationToken : t);

            return Unit.Value;
        }

        return serviceProvider
            .GetServices<IPipelineBehavior<TCommand, Unit>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate<Unit>) Handler,
                (next, pipeline) => (t) => pipeline.Handle((TCommand) command, next, t == default ? cancellationToken : t))();
    }
}

public class QueryRequestHandlerWrapperImpl<TQuery, TResponse> : QueryRequestHandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider,
        CancellationToken cancellationToken) =>
        await Handle((IQuery<TResponse>) request, serviceProvider, cancellationToken).ConfigureAwait(false);

    public override Task<TResponse> Handle(IQuery<TResponse> query, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Task<TResponse> Handler(CancellationToken t = default) => serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>()
            .Handle((TQuery) query, t == default ? cancellationToken : t);

        return serviceProvider
            .GetServices<IPipelineBehavior<TQuery, TResponse>>()
            .Reverse()
            .Aggregate((RequestHandlerDelegate<TResponse>) Handler,
                (next, pipeline) => (t) => pipeline.Handle((TQuery) query, next, t == default ? cancellationToken : t))();
    }
}
