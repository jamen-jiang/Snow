using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.DependencyInjection;
using Rongia.EventBus.EventHandlers;
using Rongia.Modularity;

namespace Rongia.EventBus;

/// <summary>
/// 事件总线模块:登记默认发布策略,并用 Scrutor 扫描全部模块程序集,
/// 把实现 <see cref="IEventHandler{TEvent}"/> 的类型登记成对应的处理器接口
/// </summary>
public sealed class RongiaEventBusModule : RongiaModule
{
    /// <summary>
    /// 登记发布策略与全部事件处理器
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 发布策略有多个实现,生命周期与生效实现都显式登记,不依赖约定扫描
        context.Services.TryAddSingleton<IEventHandlerPublisher, ForeachAwaitEventHandlerPublisher>();

        Assembly[] assemblies = context.Services
            .GetSingletonInstance<IModuleContainer>()
            .Modules
            .SelectMany(module => module.AllAssemblies)
            .Distinct()
            .ToArray();

        // 处理器只按 IEventHandler<TEvent> 登记: 事件总线在发布时按该接口从容器解析全部实现
        context.Services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo<IEventHandler>())
            .As(GetEventHandlerInterfaces)
            .WithTransientLifetime());
    }

    /// <summary>
    /// 取类型实现的全部事件处理器接口
    /// </summary>
    /// <param name="type">处理器实现类型</param>
    /// <returns>事件处理器接口集合</returns>
    private static IEnumerable<Type> GetEventHandlerInterfaces(Type type)
    {
        return type.GetInterfaces()
            .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEventHandler<>));
    }
}
