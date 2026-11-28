using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.EventBus;

/// <summary>
/// 事件总线模块: 按 <see cref="SnowEventBusOptions.Assemblies"/> 扫描并登记全部事件处理器。
/// </summary>
public sealed class SnowEventBusModule : SnowModule
{
    /// <inheritdoc />
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SnowEventBusOptions options = context.Services.GetOptions<SnowEventBusOptions>();

        // 处理器只按 IEventHandler<TEvent> 登记: 事件总线在发布时按该接口从容器解析全部实现。
        // 不能交给约定注册: 默认推断要求接口名去掉 I 之后是实现类名的后缀(OrderCreatedHandler),
        // 而这里的实现类名是任意业务命名, 只能按接口可赋值性全量捞。
        Type openHandlerContract = typeof(IEventHandler<>);
        foreach (Type type in options.Assemblies.SelectMany(AssemblyTypeCache.GetTypes))
        {
            if (!type.IsClass || type.IsAbstract || !type.IsVisible)
            {
                continue;
            }

            foreach (Type contract in type.GetInterfaces())
            {
                if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != openHandlerContract)
                {
                    continue;
                }

                context.Services.Add(new ServiceDescriptor(contract, type, options.HandlerLifetime));
            }
        }
    }
}
