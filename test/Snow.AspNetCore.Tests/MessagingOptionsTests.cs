using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.Castle;

using ServiceProvider = Snow.AspNetCore.Tests.TestServiceProvider;
using Snow.Messaging;
using Snow.Modularity;
using Snow.Uow;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// SnowMessagingOptions 的开关行为: 默认命令自动带工作单元, 关闭后命令裸跑; 处理器按显式注册的程序集扫描。
/// </summary>
public class MessagingOptionsTests
{
    /// <summary>
    /// 默认配置(未改动选项)下: 命令处理器即便没贴 [UnitOfWork], 也会被 UoW 行为包进工作单元
    /// </summary>
    [Fact]
    public async Task Command_GetsUnitOfWork_ByDefault()
    {
        NoUowProbeCommandHandler.LastHadUnitOfWork = null;
        using ServiceProvider provider = await BuildProviderAsync<DefaultMessagingModule>();

        await provider.GetRequiredService<ICommandBus>().SendAsync(new NoUowProbeCommand());

        Assert.NotNull(NoUowProbeCommandHandler.LastHadUnitOfWork);
        Assert.True(NoUowProbeCommandHandler.LastHadUnitOfWork!.Value);
    }

    /// <summary>
    /// 模块里把 AutoUnitOfWorkForCommands 关掉: 没有 [UnitOfWork] 的命令处理器取不到环境工作单元
    /// </summary>
    [Fact]
    public async Task Command_HasNoUnitOfWork_WhenAutoUnitOfWorkDisabled()
    {
        NoUowProbeCommandHandler.LastHadUnitOfWork = null;
        using ServiceProvider provider = await BuildProviderAsync<AutoUowDisabledByModule>();

        await provider.GetRequiredService<ICommandBus>().SendAsync(new NoUowProbeCommand());

        Assert.NotNull(NoUowProbeCommandHandler.LastHadUnitOfWork);
        Assert.False(NoUowProbeCommandHandler.LastHadUnitOfWork!.Value);
    }

    private static async Task<ServiceProvider> BuildProviderAsync<TModule>() where TModule : SnowModule
    {
        ServiceCollection services = new();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        await services.AddApplicationAsync<TModule>();

        return new ServiceProvider(services.BuildServiceProviderFromFactory());
    }

    /// <summary>
    /// 默认模块: 不覆盖任何选项
    /// </summary>
    [DependsOn(typeof(SnowMessagingModule))]
    [DependsOn(typeof(SnowUowModule))]
    [DependsOn(typeof(SnowCastleCoreModule))]
    private sealed class DefaultMessagingModule : SnowModule
    {
        /// <inheritdoc />
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            Configure<SnowMessagingOptions>(o => o.RegisterServicesFromAssemblyContaining<MessagingOptionsTests>());
        }
    }

    /// <summary>
    /// 关掉“命令自动开工作单元”的模块
    /// </summary>
    [DependsOn(typeof(SnowMessagingModule))]
    [DependsOn(typeof(SnowUowModule))]
    [DependsOn(typeof(SnowCastleCoreModule))]
    private sealed class AutoUowDisabledByModule : SnowModule
    {
        /// <inheritdoc />
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            Configure<SnowMessagingOptions>(o =>
            {
                o.AutoUnitOfWorkForCommands = false;
                o.RegisterServicesFromAssemblyContaining<MessagingOptionsTests>();
            });
        }
    }

    /// <summary>
    /// 探针命令
    /// </summary>
    public sealed class NoUowProbeCommand : ICommand
    {
    }

    /// <summary>
    /// 探针命令处理器: 不贴 [UnitOfWork], 只记录处理时有没有环境工作单元
    /// </summary>
    public sealed class NoUowProbeCommandHandler : ICommandHandler<NoUowProbeCommand>
    {
        /// <summary>
        /// 最近一次处理时是否有环境工作单元
        /// </summary>
        public static bool? LastHadUnitOfWork { get; set; }

        private readonly IUnitOfWorkManager _unitOfWorkManager;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="unitOfWorkManager">工作单元管理器</param>
        public NoUowProbeCommandHandler(IUnitOfWorkManager unitOfWorkManager)
        {
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <inheritdoc />
        public Task HandleAsync(NoUowProbeCommand command, CancellationToken cancellationToken)
        {
            LastHadUnitOfWork = _unitOfWorkManager.Current is not null;
            return Task.CompletedTask;
        }
    }
}
