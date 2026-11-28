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
/// 命令/查询处理器走约定注册管道后自动带工作单元: 命令成功即提交、抛异常即回滚、查询不参与。
/// <para>处理器不写任何服务注解, 由 <c>CommandHandlerConventionalRegistrar</c> 交给约定注册;
/// 类上贴 [UnitOfWork] 后, 工作单元拦截器就会挂上。</para>
/// </summary>
public class MessagingUowTests
{
    /// <summary>
    /// 命令正常返回: 数据被保存、事务被提交、事务对象被释放
    /// </summary>
    [Fact]
    public async Task Command_CommitsOnSuccess()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        await provider.GetRequiredService<ICommandBus>().SendAsync(new ProbeCommand());

        Assert.NotNull(ProbeDatabaseApi.Last);
        Assert.Equal(1, ProbeDatabaseApi.Last!.SaveCount);
        Assert.NotNull(ProbeTransactionApi.Last);
        Assert.Equal(1, ProbeTransactionApi.Last!.CommitCount);
        Assert.Equal(0, ProbeTransactionApi.Last!.RollbackCount);
        Assert.True(ProbeTransactionApi.Last!.IsDisposed);
    }

    /// <summary>
    /// 命令抛异常: 不保存、不提交、回滚
    /// </summary>
    [Fact]
    public async Task Command_RollsBackOnException()
    {
        using ServiceProvider provider = await BuildProviderAsync();
        ICommandBus commandBus = provider.GetRequiredService<ICommandBus>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            commandBus.SendAsync(new ProbeCommand { ShouldThrow = true }));

        Assert.NotNull(ProbeDatabaseApi.Last);
        Assert.Equal(0, ProbeDatabaseApi.Last!.SaveCount);
        Assert.NotNull(ProbeTransactionApi.Last);
        Assert.Equal(0, ProbeTransactionApi.Last!.CommitCount);
        Assert.Equal(1, ProbeTransactionApi.Last!.RollbackCount);
    }

    /// <summary>
    /// 查询不参与: 处理器里取不到环境工作单元
    /// </summary>
    [Fact]
    public async Task Query_DoesNotStartUnitOfWork()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        int result = await provider.GetRequiredService<IQueryBus>().QueryAsync(new ProbeQuery());

        Assert.Equal(0, result);
    }

    /// <summary>
    /// 处理器由约定注册登记: 解析得到的是被代理的对象, 方法执行前后各经过一次工作单元拦截
    /// </summary>
    [Fact]
    public async Task CommandHandler_IsRegisteredByConvention()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        ICommandHandler<ProbeCommand> handler = provider.GetRequiredService<ICommandHandler<ProbeCommand>>();

        Assert.NotEqual(typeof(ProbeCommandHandler), handler.GetType());
        await handler.HandleAsync(new ProbeCommand(), CancellationToken.None);
        Assert.Equal(1, ProbeDatabaseApi.Last!.SaveCount);
    }

    private static async Task<ServiceProvider> BuildProviderAsync()
    {
        ServiceCollection services = new();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        await services.AddApplicationAsync<MessagingUowTestModule>();

        return new ServiceProvider(services.BuildServiceProviderFromFactory());
    }

    /// <summary>
    /// 测试模块: 消息 + 工作单元 + Castle 代理
    /// </summary>
    [DependsOn(typeof(SnowMessagingModule))]
    [DependsOn(typeof(SnowUowModule))]
    [DependsOn(typeof(SnowCastleCoreModule))]

    private sealed class MessagingUowTestModule : SnowModule
    {
    }

    /// <summary>
    /// 探针命令: 可要求处理时抛异常
    /// </summary>
    public sealed class ProbeCommand : ICommand
    {
        /// <summary>
        /// 是否在处理时抛异常
        /// </summary>
        public bool ShouldThrow { get; init; }
    }

    /// <summary>
    /// 探针命令处理器: 类上贴 [UnitOfWork] 自动带工作单元;
    /// 方法必须是 virtual, Castle 类代理才拦得到
    /// </summary>
    [UnitOfWork]
    public class ProbeCommandHandler : ICommandHandler<ProbeCommand>
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="unitOfWorkManager">工作单元管理器</param>
        public ProbeCommandHandler(IUnitOfWorkManager unitOfWorkManager)
        {
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <inheritdoc />
        public virtual Task HandleAsync(ProbeCommand command, CancellationToken cancellationToken)
        {
            IUnitOfWork unitOfWork = _unitOfWorkManager.Current
                ?? throw new InvalidOperationException("命令没有运行在工作单元里。");

            unitOfWork.GetOrAddApi(() => new ProbeDatabaseApi());
            unitOfWork.GetOrAddApi(() => new ProbeTransactionApi());

            if (command.ShouldThrow)
            {
                throw new InvalidOperationException("模拟业务异常。");
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 探针查询
    /// </summary>
    public sealed class ProbeQuery : IQuery<int>
    {
    }

    /// <summary>
    /// 探针查询处理器: 没有环境工作单元时返回 0, 有则返回 1
    /// </summary>
    public sealed class ProbeQueryHandler : IQueryHandler<ProbeQuery, int>
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="unitOfWorkManager">工作单元管理器</param>
        public ProbeQueryHandler(IUnitOfWorkManager unitOfWorkManager)
        {
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <inheritdoc />
        public Task<int> HandleAsync(ProbeQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(_unitOfWorkManager.Current is null ? 0 : 1);
        }
    }

    /// <summary>
    /// 探针数据库 API: 记录保存次数
    /// </summary>
    public sealed class ProbeDatabaseApi : ISupportsSavingChanges
    {
        /// <summary>
        /// 最近一次创建的实例
        /// </summary>
        public static ProbeDatabaseApi? Last { get; private set; }

        /// <summary>
        /// 构造器
        /// </summary>
        public ProbeDatabaseApi()
        {
            Last = this;
        }

        /// <summary>
        /// 保存次数
        /// </summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc />
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 探针事务 API: 记录提交/回滚次数与释放状态
    /// </summary>
    public sealed class ProbeTransactionApi : ITransactionApi, ISupportsRollback
    {
        /// <summary>
        /// 最近一次创建的实例
        /// </summary>
        public static ProbeTransactionApi? Last { get; private set; }

        /// <summary>
        /// 构造器
        /// </summary>
        public ProbeTransactionApi()
        {
            Last = this;
        }

        /// <summary>
        /// 提交次数
        /// </summary>
        public int CommitCount { get; private set; }

        /// <summary>
        /// 回滚次数
        /// </summary>
        public int RollbackCount { get; private set; }

        /// <summary>
        /// 是否已释放
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCount++;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCount++;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
