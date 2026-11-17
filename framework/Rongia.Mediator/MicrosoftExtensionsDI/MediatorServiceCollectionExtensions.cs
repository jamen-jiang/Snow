using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rongia.Mediator;
using Rongia.Mediator.Pipeline;
using Rongia.Mediator.Registration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to scan for Mediator handlers and registers them.
/// - Scans for any handler interface implementations and registers them as <see cref="ServiceLifetime.Transient"/>
/// - Scans for any <see cref="IRequestPreProcessor{TRequest}"/> and <see cref="IRequestPostProcessor{TRequest,TResponse}"/> implementations and registers them as transient instances
/// Registers <see cref="ICommandBus"/> and <see cref="IQueryBus"/> as transient instances
/// After calling AddMediator you can use the container to resolve an <see cref="ICommandBus"/> or <see cref="IQueryBus"/> instance.
/// This does not scan for any <see cref="IPipelineBehavior{TRequest,TResponse}"/> instances including <see cref="RequestPreProcessorBehavior{TRequest,TResponse}"/> and <see cref="RequestPreProcessorBehavior{TRequest,TResponse}"/>.
/// To register behaviors, use the <see cref="ServiceCollectionServiceExtensions.AddTransient(IServiceCollection,Type,Type)"/> with the open generic or closed generic types.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers handlers and mediator types from the specified assemblies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">The action used to configure the options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, 
        Action<MediatorServiceConfiguration> configuration)
    {
        var serviceConfig = new MediatorServiceConfiguration();

        configuration.Invoke(serviceConfig);

        return services.AddMediator(serviceConfig);
    }
    
    /// <summary>
    /// Registers handlers and mediator types from the specified assemblies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, 
        MediatorServiceConfiguration configuration)
    {
        if (!configuration.AssembliesToRegister.Any())
        {
            throw new ArgumentException("No assemblies found to scan. Supply at least one assembly to scan for handlers.");
        }

        ServiceRegistrar.SetGenericRequestHandlerRegistrationLimitations(configuration);

        ServiceRegistrar.AddMediatorClassesWithTimeout(services, configuration);

        ServiceRegistrar.AddRequiredServices(services, configuration);

        return services;
    }
}
