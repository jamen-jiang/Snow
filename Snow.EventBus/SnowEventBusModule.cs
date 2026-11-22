using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;
using Snow.EventBus.EventHandlers;
using Snow.Modularity;

namespace Snow.EventBus;

/// <summary>
/// 事件总线模块:登记默认发布策略,并扫描全部模块程序集,
/// 把实现 <see cref="IEventHandler{TEvent}"/> 的类型登记成对应的处理器接口
/// </summary>
public sealed class SnowEventBusModule : SnowModule
{
    /// <summary>
    /// 登记发布策略与全部事件处理器
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 默认发布策略由 ForeachAwaitEventHandlerPublisher 上的 [SingletonService] 登记: 其它策略(TaskWhenAll 等)
        // 没有注解, 不会被误登记成第二条, 宿主想换就自己 Add 一份

        Assembly[] assemblies = context.Services
            .GetSingletonInstance<IModuleContainer>()
            .Modules
            .SelectMany(module => module.AllAssemblies)
            .Distinct()
            .ToArray();

        // 处理器只按 IEventHandler<TEvent> 登记: 事件总线在发布时按该接口从容器解析全部实现。
        // 不能交给约定注册: 默认推断要求接口名去掉 I 之后是实现类名的后缀(OrderCreatedHandler),
        // 而这里的实现类名是任意业务命名, 只能按接口可赋值性全量捞
        foreach (Type type in assemblies.SelectMany(AssemblyTypeCache.GetTypes))
        {
            if (!type.IsClass || type.IsAbstract || !type.IsVisible)
            {
                continue;
            }

            foreach (Type contract in type.GetInterfaces())
            {
                if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != typeof(IEventHandler<>))
                {
                    continue;
                }

                context.Services.AddTransient(contract, type);
            }
        }
    }
}
