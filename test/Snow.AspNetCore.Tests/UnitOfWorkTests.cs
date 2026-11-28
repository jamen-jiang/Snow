using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.Castle;

using ServiceProvider = Snow.AspNetCore.Tests.TestServiceProvider;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.Uow;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 工作单元: 嵌套 Begin 返回子对象不误伤外层、特性可标在接口方法上、IsDisabled 能关掉、类级特性自动开启。
/// </summary>
public class UnitOfWorkTests
{
    /// <summary>
    /// 已在外层工作单元里再次 Begin 拿到的是子对象: 子对象提交/释放都不影响外层
    /// </summary>
    [Fact]
    public async Task NestedBegin_ReturnsChild_AndDoesNotAffectOuter()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        IUnitOfWorkManager manager = provider.GetRequiredService<IUnitOfWorkManager>();

        IUnitOfWork outer = manager.Begin();
        IUnitOfWork inner = manager.Begin();

        Assert.NotSame(outer, inner);
        Assert.Equal(outer.Id, inner.Id);

        // 子对象提交是空操作
        await inner.CompleteAsync();
        Assert.True(outer.IsActive);
        Assert.False(outer.IsCompleted);

        // 子对象释放也不影响外层
        await inner.DisposeAsync();
        Assert.True(outer.IsActive);

        await outer.CompleteAsync();
        Assert.True(outer.IsCompleted);
        Assert.False(outer.IsActive);
    }

    /// <summary>
    /// 特性标在接口方法上也要生效(拦截器拿到的是实现方法, 靠接口兜底命中)
    /// </summary>
    [Fact]
    public async Task AttributeOnInterfaceMethod_StartsUnitOfWork()
    {
        PlaceOrderProbe.SawUnitOfWork = false;
        using ServiceProvider provider = await BuildProviderAsync();

        IPlaceOrderService service = provider.GetRequiredService<IPlaceOrderService>();
        service.Place();

        Assert.True(PlaceOrderProbe.SawUnitOfWork);
    }

    /// <summary>
    /// IsDisabled = true 时不开工作单元
    /// </summary>
    [Fact]
    public async Task IsDisabled_DoesNotStartUnitOfWork()
    {
        DisabledProbe.SawUnitOfWork = false;
        using ServiceProvider provider = await BuildProviderAsync();

        IDisabledUowService service = provider.GetRequiredService<IDisabledUowService>();
        service.Run();

        Assert.False(DisabledProbe.SawUnitOfWork);
    }

    /// <summary>
    /// 类上贴 [UnitOfWork] 后整类自动开工作单元
    /// </summary>
    [Fact]
    public async Task ClassLevelAttribute_StartsUnitOfWork()
    {
        ConventionProbe.SawUnitOfWork = false;
        using ServiceProvider provider = await BuildProviderAsync();

        IConventionService service = provider.GetRequiredService<IConventionService>();
        service.Execute();

        Assert.True(ConventionProbe.SawUnitOfWork);
    }

    /// <summary>
    /// 每个工作单元拿到自己的 DI 作用域: Scoped 服务互不共享, 工作单元释放时作用域一起销毁
    /// </summary>
    [Fact]
    public async Task EachUnitOfWork_GetsItsOwnScope()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        IUnitOfWorkManager manager = provider.GetRequiredService<IUnitOfWorkManager>();

        IUnitOfWork first = manager.Begin();
        ScopeProbe firstProbe = first.ServiceProvider.GetRequiredService<ScopeProbe>();
        Assert.False(firstProbe.IsDisposed);

        await first.CompleteAsync();
        await first.DisposeAsync();
        Assert.True(firstProbe.IsDisposed);

        IUnitOfWork second = manager.Begin();
        ScopeProbe secondProbe = second.ServiceProvider.GetRequiredService<ScopeProbe>();
        Assert.NotSame(firstProbe, secondProbe);
        Assert.False(secondProbe.IsDisposed);

        await second.RollbackAsync();
        await second.DisposeAsync();
        Assert.True(secondProbe.IsDisposed);
    }

    /// <summary>
    /// 数据库 API 统一保存、事务 API 统一提交, 释放时销毁事务
    /// </summary>
    [Fact]
    public async Task DatabaseAndTransactionApis_AreSavedCommittedAndDisposed()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        IUnitOfWorkManager manager = provider.GetRequiredService<IUnitOfWorkManager>();

        IUnitOfWork unitOfWork = manager.Begin(new UnitOfWorkOptions { IsTransactional = true });

        ProbeDatabaseApi databaseApi = unitOfWork.GetOrAddApi(() => new ProbeDatabaseApi());
        ProbeDatabaseApi sameDatabaseApi = unitOfWork.GetOrAddApi<ProbeDatabaseApi>(() => throw new InvalidOperationException("不应该再建一次"));
        Assert.Same(databaseApi, sameDatabaseApi);

        ProbeTransactionApi transactionApi = unitOfWork.GetOrAddApi(() => new ProbeTransactionApi());
        ProbeTransactionApi sameTransactionApi = unitOfWork.GetOrAddApi<ProbeTransactionApi>(() => throw new InvalidOperationException("不应该再建一次"));
        Assert.Same(transactionApi, sameTransactionApi);

        await unitOfWork.SaveChangesAsync();
        Assert.Equal(1, databaseApi.SaveCount);

        await unitOfWork.CompleteAsync();
        Assert.Equal(1, transactionApi.CommitCount);
        Assert.True(unitOfWork.IsCompleted);

        await unitOfWork.DisposeAsync();
        Assert.True(transactionApi.IsDisposed);
    }

    private static async Task<ServiceProvider> BuildProviderAsync()
    {
        ServiceCollection services = new();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        await services.AddApplicationAsync<UowTestModule>();

        return new ServiceProvider(services.BuildServiceProviderFromFactory());
    }

    /// <summary>
    /// 测试模块: 依赖工作单元模块(自带登记器)与 AOP 模块(提供代理工厂)
    /// </summary>
    [DependsOn(typeof(SnowUowModule))]
    [DependsOn(typeof(SnowCastleCoreModule))]

    private sealed class UowTestModule : SnowModule
    {
    }

    /// <summary>
    /// 下单契约: 特性标在接口方法上
    /// </summary>
    public interface IPlaceOrderService
    {
        /// <summary>
        /// 下单
        /// </summary>
        [UnitOfWork]
        void Place();
    }

    /// <summary>
    /// 下单实现: 记录调用时是否存在环境工作单元
    /// </summary>
    [TransientService]
    [ExposeServices(typeof(IPlaceOrderService))]
    public sealed class PlaceOrderService : IPlaceOrderService
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="unitOfWorkManager">工作单元管理器</param>
        public PlaceOrderService(IUnitOfWorkManager unitOfWorkManager)
        {
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <inheritdoc />
        public void Place()
        {
            PlaceOrderProbe.SawUnitOfWork = _unitOfWorkManager.Current is not null;
        }
    }

    /// <summary>
    /// 下单探针
    /// </summary>
    public static class PlaceOrderProbe
    {
        /// <summary>
        /// 调用时是否看到了工作单元
        /// </summary>
        public static bool SawUnitOfWork;
    }

    /// <summary>
    /// 被禁用工作单元的契约
    /// </summary>
    public interface IDisabledUowService
    {
        /// <summary>
        /// 执行
        /// </summary>
        [UnitOfWork(IsDisabled = true)]
        void Run();
    }

    /// <summary>
    /// 被禁用工作单元的实现
    /// </summary>
    [TransientService]
    [ExposeServices(typeof(IDisabledUowService))]
    public sealed class DisabledUowService : IDisabledUowService
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="unitOfWorkManager">工作单元管理器</param>
        public DisabledUowService(IUnitOfWorkManager unitOfWorkManager)
        {
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <inheritdoc />
        public void Run()
        {
            DisabledProbe.SawUnitOfWork = _unitOfWorkManager.Current is not null;
        }
    }

    /// <summary>
    /// 禁用探针
    /// </summary>
    public static class DisabledProbe
    {
        /// <summary>
        /// 调用时是否看到了工作单元
        /// </summary>
        public static bool SawUnitOfWork;
    }

    /// <summary>
    /// 约定契约
    /// </summary>
    public interface IConventionService
    {
        /// <summary>
        /// 执行
        /// </summary>
        void Execute();
    }

    /// <summary>
    /// 约定实现: 类上贴 [UnitOfWork], 整类每个公开方法都在工作单元里
    /// </summary>
    [UnitOfWork]
    [TransientService]
    [ExposeServices(typeof(IConventionService))]
    public sealed class ConventionService : IConventionService
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="unitOfWorkManager">工作单元管理器</param>
        public ConventionService(IUnitOfWorkManager unitOfWorkManager)
        {
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <inheritdoc />
        public void Execute()
        {
            ConventionProbe.SawUnitOfWork = _unitOfWorkManager.Current is not null;
        }
    }

    /// <summary>
    /// 数据库 API 探针: 记录被保存/回滚的次数
    /// </summary>
    private sealed class ProbeDatabaseApi : ISupportsSavingChanges, ISupportsRollback
    {
        /// <summary>
        /// 保存次数
        /// </summary>
        public int SaveCount { get; private set; }

        /// <summary>
        /// 回滚次数
        /// </summary>
        public int RollbackCount { get; private set; }

        /// <inheritdoc />
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCount++;

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 事务 API 探针: 记录提交次数与是否被销毁
    /// </summary>
    private sealed class ProbeTransactionApi : ITransactionApi, ISupportsRollback
    {
        /// <summary>
        /// 提交次数
        /// </summary>
        public int CommitCount { get; private set; }

        /// <summary>
        /// 回滚次数
        /// </summary>
        public int RollbackCount { get; private set; }

        /// <summary>
        /// 是否已销毁
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

    /// <summary>
    /// 作用域探针: 按作用域创建, 用来验证每个工作单元有自己的作用域
    /// </summary>
    [ScopedService]
    public sealed class ScopeProbe : IDisposable
    {
        /// <summary>
        /// 是否已被释放
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    /// <summary>
    /// 约定探针
    /// </summary>
    public static class ConventionProbe
    {
        /// <summary>
        /// 调用时是否看到了工作单元
        /// </summary>
        public static bool SawUnitOfWork;
    }
}