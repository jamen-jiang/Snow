using Assembly = System.Reflection.Assembly;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Messaging.Registration;
using Snow.Modularity;

namespace Snow.Messaging;

/// <summary>
/// 消息派发模块: 取模块清单里的程序集, 扫描并注册命令/查询处理器与两条总线。
/// 本程序集同时是约定注入的扫描锚点, <see cref="CommandBus"/> 与 <see cref="QueryBus"/> 靠生命周期标记自动登记
/// </summary>
public sealed class SnowMessagingModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IModuleContainer container = context.Services.GetSingletonInstance<IModuleContainer>();

        Assembly[] assemblies = container.Modules
            .SelectMany(module => module.AllAssemblies)
            .Distinct()
            .ToArray();

        // 处理器登记器: 让处理器走约定注册管道, 模块挂的拦截器(如工作单元)才能作用到处理器上
        context.Services.AddConventionalRegistrar(new CommandHandlerConventionalRegistrar());

        ServiceRegistrar.AddMessagingClassesWithTimeout(context.Services, assemblies);
    }
}
