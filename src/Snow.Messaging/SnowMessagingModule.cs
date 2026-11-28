using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Messaging.Registration;
using Snow.Modularity;
using Snow.Uow;

namespace Snow.Messaging;

/// <summary>
/// 消息派发模块: 按 <see cref="SnowMessagingOptions.Assemblies"/> 扫描并注册命令/查询处理器、两条总线与管道行为(程序集为空即不注册任何处理器)。
/// <para>处理器按 MediatR 方式登记为瞬态实现(无约定注册、无 Castle 代理);
/// 工作单元由命令总线的 <c>UnitOfWorkBehavior</c> 统一包裹。</para>
/// </summary>
[DependsOn(typeof(SnowUowModule))]
public sealed class SnowMessagingModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var services = context.Services;

        // 选项由宿主在 PreConfigureServices 里 Configure<SnowMessagingOptions>(...) 登记(Configure 内部已 AddOptions);
        // 这里直接取结算快照。默认值写在 SnowMessagingOptions 的属性初始值里。
        var options = services.GetOptions<SnowMessagingOptions>();

        ServiceRegistrar.AddMessagingClassesWithTimeout(services, options.Assemblies);

        // 工作单元行为先登记 -> 位于外层(先进入、后退出), 事务包住整条管道;
        // 额外行为随后登记 -> 位于内层, 先于处理器执行、后于处理器退出。
        if (options.AutoUnitOfWorkForCommands)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        }

        foreach (SnowMessagingBehavior behavior in options.Behaviors)
        {
            services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behavior.Type, behavior.Lifetime));
        }
    }
}
