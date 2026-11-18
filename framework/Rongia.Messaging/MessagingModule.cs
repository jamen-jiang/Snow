using Assembly = System.Reflection.Assembly;
using Microsoft.Extensions.DependencyInjection;
using Rongia.Modularity;

namespace Rongia.Messaging;

/// <summary>
/// 消息派发模块: 自己装配命令/查询总线, 扫描范围取自模块清单。
/// 同时作为本程序集进入约定注入扫描的锚点, CommandBus 与 QueryBus 靠生命周期标记自动登记
/// </summary>
public sealed class MessagingModule : Module
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Assembly[] assemblies = context.ModuleAssemblies.ToArray();

        context.Services.AddMessaging(configuration => configuration.RegisterServicesFromAssemblies(assemblies));
    }
}
