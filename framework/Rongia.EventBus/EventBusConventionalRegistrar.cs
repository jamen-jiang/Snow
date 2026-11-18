using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.DependencyInjection;

namespace Rongia.EventBus;

/// <summary>
/// 事件处理器的约定注册: 凡实现 <c>IEventHandler&lt;TEvent&gt;</c> 的类都会被登记,
/// 既是实现类型本身, 也是该事件对应的处理器接口
/// </summary>
/// <remarks>
/// 处理器清单不再写进配置对象: 事件总线在发布时按 <c>IEventHandler&lt;TEvent&gt;</c> 从容器解析,
/// 因此这里只负责把类型正确登记进容器
/// </remarks>
public sealed class EventBusConventionalRegistrar : ConventionalRegistrarBase
{
    /// <inheritdoc />
    public override void AddType(IServiceCollection services, Type type)
    {
        Type[] eventTypes = GetEventTypes(type);

        if (eventTypes.Length == 0)
        {
            return;
        }

        // 处理器按实现类型解析: 事件总线为每个处理器建独立作用域后按类型取实例
        services.TryAddTransient(type, type);

        foreach (Type eventType in eventTypes)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventHandler<>).MakeGenericType(eventType), type));
        }
    }

    /// <summary>
    /// 取类型实现的事件类型
    /// </summary>
    /// <param name="type">处理器实现类型</param>
    /// <returns>事件类型数组, 未实现任何事件处理器接口时为空数组</returns>
    private static Type[] GetEventTypes(Type type)
    {
        return type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
            .Select(i => i.GetGenericArguments()[0])
            .Distinct()
            .ToArray();
    }
}
