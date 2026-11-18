using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.DependencyInjection;
using Rongia.EventBus.EventHandlers;
using Rongia.Modularity;

namespace Rongia.EventBus;

/// <summary>
/// 事件总线模块: 登记默认发布策略与事件处理器约定注册器,
/// 处理器由 <see cref="EventBusConventionalRegistrar"/> 按 <see cref="IEventHandler{TEvent}"/> 自动登记
/// </summary>
public sealed class EventBusModule : Module
{
    /// <summary>
    /// 登记约定注册器: 必须早于任何程序集扫描, 所以放在前置阶段
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddConventionalRegistrar<EventBusConventionalRegistrar>();
    }

    /// <summary>
    /// 指定默认发布策略: 有多个实现, 生命周期与生效实现都显式登记, 不依赖约定扫描
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.TryAddSingleton<IEventHandlerPublisher, ForeachAwaitEventHandlerPublisher>();
    }
}
