using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rongia.Messaging;
using Rongia.Messaging.Registration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 扫描并注册中介处理器的扩展方法
/// <list type="bullet">
/// <item>扫描所有处理器接口的实现,按 <see cref="ServiceLifetime.Transient"/> 注册</item>
/// <item>把 <see cref="ICommandBus"/> 与 <see cref="IQueryBus"/> 注册为瞬时服务</item>
/// </list>
/// 调用 AddMessaging 之后即可从容器解析 <see cref="ICommandBus"/> 或 <see cref="IQueryBus"/>。
/// <para>注意:此方法不扫描 <see cref="IPipelineBehavior{TRequest,TResponse}"/> 及其派生行为;要注册行为请用
/// <see cref="ServiceCollectionServiceExtensions.AddTransient(IServiceCollection,Type,Type)"/> 指定开放式或闭式泛型。</para>
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// 扫描指定程序集并注册处理器与总线
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置回调</param>
    /// <returns>服务集合本身,便于链式调用</returns>
    public static IServiceCollection AddMessaging(this IServiceCollection services, 
        Action<MessagingServiceConfiguration> configuration)
    {
        var serviceConfig = new MessagingServiceConfiguration();

        configuration.Invoke(serviceConfig);

        return services.AddMessaging(serviceConfig);
    }
    
    /// <summary>
    /// 扫描指定程序集并注册处理器与总线
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合本身,便于链式调用</returns>
    /// <exception cref="ArgumentException">未提供任何待扫描程序集</exception>
    public static IServiceCollection AddMessaging(this IServiceCollection services, 
        MessagingServiceConfiguration configuration)
    {
        if (!configuration.AssembliesToRegister.Any())
        {
            throw new ArgumentException("No assemblies found to scan. Supply at least one assembly to scan for handlers.");
        }

        ServiceRegistrar.SetGenericRequestHandlerRegistrationLimitations(configuration);

        ServiceRegistrar.AddMessagingClassesWithTimeout(services, configuration);

        ServiceRegistrar.AddRequiredServices(services, configuration);

        return services;
    }
}