using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.DependencyInjection;
using Rongia.EventBus.EventHandlers;
using Rongia.Modularity;

namespace Rongia.EventBus;

/// <summary>
/// 事件总线模块: 在 Pre 阶段登记发布策略, 并用 OnRegistered 旁听约定注册来收集处理器类型。
/// 放在 Pre 是因为约定扫描发生在各模块的 ConfigureServices 之前, 钩子必须先就位;
/// 收集到的类型通过 Configure 延迟写入配置, 由事件总线构造时统一求值
/// </summary>
public sealed class EventBusModule : Module
{
    /// <inheritdoc />
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IServiceCollection services = context.Services;

        // 发布策略有多个实现, 生命周期与生效实现都显式登记, 不依赖约定扫描
        services.TryAddSingleton<IEventHandlerPublisher, ForeachAwaitEventHandlerPublisher>();

        var handlerTypes = new List<Type>();

        services.OnRegistered(registration =>
        {
            if (IsEventHandler(registration.ImplementationType))
            {
                handlerTypes.Add(registration.ImplementationType);
            }
        });

        services.Configure<EventBusOptions>(options =>
        {
            options.Handlers.AddIfNotContains(handlerTypes);
        });
    }

    /// <summary>
    /// 判断实现类型是否为事件处理器
    /// </summary>
    /// <param name="implementationType">实现类型</param>
    /// <returns>是事件处理器时为 true</returns>
    private static bool IsEventHandler(Type implementationType)
    {
        if (implementationType.IsAbstract || implementationType.IsInterface || implementationType.IsGenericTypeDefinition)
        {
            return false;
        }

        if (!typeof(IEventHandler).IsAssignableFrom(implementationType))
        {
            return false;
        }

        return implementationType.GetInterfaces()
            .Any(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEventHandler<>));
    }
}
