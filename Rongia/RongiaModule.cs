using Rongia.EventBus;
using Rongia.Messaging;
using Rongia.Modularity;

namespace Rongia;

/// <summary>
/// 宿主启动模块: 组合根, 只声明本应用由哪些模块组成
/// </summary>
[DependsOn(typeof(MessagingModule), typeof(EventBusModule))]
public sealed class RongiaModule : Module
{
}
