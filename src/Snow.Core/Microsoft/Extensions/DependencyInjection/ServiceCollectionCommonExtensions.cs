using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using Snow.Modularity;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务集合的通用扩展(对应 ABP 的 <c>ServiceCollectionCommonExtensions</c>)。
/// <para>只保留 Snow 真正需要的三件事: 单例实例读取、按工厂/默认方式构建容器、
/// 登记阶段取一次选项快照。ABP 里那些「登记期从集合反查服务」的成员
/// (IsAdded / GetService / GetRequiredService / GetServiceLazy / GetRequiredServiceLazy /
/// GetServiceProviderOrNull)在 Snow 全仓零调用, 已删除。</para>
/// </summary>
public static class ServiceCollectionCommonExtensions
{
    // GetTypeFinder 暂未移植:
    //   ABP 版为 public static ITypeFinder GetTypeFinder(this IServiceCollection services)
    //       => services.GetSingletonInstance<ITypeFinder>();
    //   Snow 目前没有 Snow.Reflection.ITypeFinder 抽象(也没有默认实现),
    //   补上 ITypeFinder 后再按上面一行原样加回即可。

    /// <summary>
    /// 取指定类型的单例, 不存在返回 null(对应 ABP 的 GetSingletonInstanceOrNull)。
    /// <para>实现走 ServiceDescriptorExtensions.NormalizedImplementationInstance, 键控/非键控都归一化处理。</para>
    /// </summary>
    public static T? GetSingletonInstanceOrNull<T>(this IServiceCollection services)
    {
        return (T?)services
            .FirstOrDefault(d => d.ServiceType == typeof(T))
            ?.NormalizedImplementationInstance();
    }

    /// <summary>
    /// 取指定类型的单例, 不存在抛异常(对应 ABP 的 GetSingletonInstance)。
    /// </summary>
    public static T GetSingletonInstance<T>(this IServiceCollection services)
    {
        var service = services.GetSingletonInstanceOrNull<T>();
        if (service == null)
        {
            throw new InvalidOperationException("Could not find singleton service: " + typeof(T).AssemblyQualifiedName);
        }

        return service;
    }

    /// <summary>
    /// 若集合里登记了 <see cref="IServiceProviderFactory{TContainerBuilder}"/> 就用它建容器, 否则退回默认
    /// <see cref="ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(IServiceCollection)"/>
    /// (对应 ABP 的 BuildServiceProviderFromFactory 无参版)。
    /// </summary>
    public static IServiceProvider BuildServiceProviderFromFactory(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var service in services)
        {
            var factoryInterface = service.NormalizedImplementationInstance()?.GetType()
                .GetTypeInfo()
                .GetInterfaces()
                .FirstOrDefault(i => i.GetTypeInfo().IsGenericType &&
                                     i.GetGenericTypeDefinition() == typeof(IServiceProviderFactory<>));

            if (factoryInterface == null)
            {
                continue;
            }

            var containerBuilderType = factoryInterface.GenericTypeArguments[0];
            return (IServiceProvider)typeof(ServiceCollectionCommonExtensions)
                .GetTypeInfo()
                .GetMethods()
                .Single(m => m.Name == nameof(BuildServiceProviderFromFactory) && m.IsGenericMethod)
                .MakeGenericMethod(containerBuilderType)
                .Invoke(null, new object?[] { services, null })!;
        }

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 用集合里登记的具体 <see cref="IServiceProviderFactory{TContainerBuilder}"/> 构建服务提供者
    /// (对应 ABP 的 BuildServiceProviderFromFactory&lt;TContainerBuilder&gt;)。
    /// </summary>
    public static IServiceProvider BuildServiceProviderFromFactory<TContainerBuilder>(this IServiceCollection services, Action<TContainerBuilder>? builderAction = null) where TContainerBuilder : notnull
    {
        ArgumentNullException.ThrowIfNull(services);

        var serviceProviderFactory = services.GetSingletonInstanceOrNull<IServiceProviderFactory<TContainerBuilder>>();
        if (serviceProviderFactory == null)
        {
            throw new InvalidOperationException($"Could not find {typeof(IServiceProviderFactory<TContainerBuilder>).FullName} in {services}.");
        }

        var builder = serviceProviderFactory.CreateBuilder(services);
        builderAction?.Invoke(builder);
        return serviceProviderFactory.CreateServiceProvider(builder);
    }

    /// <summary>
    /// 在服务登记阶段解算一次选项快照: 配置节基线 + 已登记的 Configure/PostConfigure 动作全部生效后的值。
    /// <para>供必须在 <c>ConfigureServices</c> 里立刻读选项值来决定服务登记的模块使用; 内部临时建一次容器解算 <see cref="IOptions{TOptions}"/>, 用完立即释放。</para>
    /// </summary>
    /// <typeparam name="T">选项类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>结算后的选项</returns>
    public static T GetOptions<T>(this IServiceCollection services) where T : class
    {
        ArgumentNullException.ThrowIfNull(services);

        IServiceProvider provider = services.BuildServiceProviderFromFactory();

        try
        {
            return provider.GetRequiredService<IOptions<T>>().Value;
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }
}
