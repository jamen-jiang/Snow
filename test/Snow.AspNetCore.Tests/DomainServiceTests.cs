using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;
using Snow.Domain;
using Snow.Domain.Services;
using Snow.Guids;
using Snow.Modularity;
using Snow.Timing;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 领域服务基类: 惰性服务提供者由容器填进来, 常用依赖按需解析
/// </summary>
public class DomainServiceTests
{
    /// <summary>
    /// 约定注册把基类要求的惰性服务提供者从容器传进构造器: 解析出来的领域服务已经拿到它
    /// </summary>
    [Fact]
    public async Task DomainService_GetsLazyServiceProviderFromConventionalRegistration()
    {
        using ServiceProvider provider = await BuildProviderAsync();
        using IServiceScope scope = provider.CreateScope();

        ProbeDomainService domainService = scope.ServiceProvider.GetRequiredService<ProbeDomainService>();

        Assert.NotNull(domainService.LazyServiceProvider);
        Assert.IsType<SnowLazyServiceProvider>(domainService.LazyServiceProvider);
    }

    /// <summary>
    /// 基类上的常用依赖都能取到: 时钟、Guid 生成器、日志、异步执行器
    /// </summary>
    [Fact]
    public async Task DomainService_ResolvesCommonDependencies()
    {
        using ServiceProvider provider = await BuildProviderAsync();
        using IServiceScope scope = provider.CreateScope();

        ProbeDomainService domainService = scope.ServiceProvider.GetRequiredService<ProbeDomainService>();

        Assert.NotNull(domainService.ReadClock());
        Assert.NotEqual(Guid.Empty, domainService.ReadGuid());
        Assert.NotNull(domainService.ReadLogger());
        Assert.NotNull(domainService.ReadAsyncExecuter());
    }

    /// <summary>
    /// 惰性服务提供者把解析结果缓存下来: 同一个服务类型取两次拿到同一个实例
    /// </summary>
    [Fact]
    public async Task LazyServiceProvider_CachesResolvedServices()
    {
        using ServiceProvider provider = await BuildProviderAsync();
        using IServiceScope scope = provider.CreateScope();

        ISnowLazyServiceProvider lazyServiceProvider = scope.ServiceProvider.GetRequiredService<ISnowLazyServiceProvider>();

        Assert.Same(
            lazyServiceProvider.LazyGetRequiredService<IClock>(),
            lazyServiceProvider.LazyGetRequiredService<IClock>());
    }

    /// <summary>
    /// 服务标识把"服务类型 + 键"合成一个键: 类型相同但键不同就不是同一个服务
    /// </summary>
    [Fact]
    public void ServiceIdentifier_DistinguishesByKey()
    {
        ServiceIdentifier withoutKey = new(typeof(IClock));
        ServiceIdentifier withKey = new("key", typeof(IClock));

        Assert.NotEqual(withoutKey, withKey);
        Assert.Equal(withKey, new ServiceIdentifier("key", typeof(IClock)));
        Assert.NotEqual(withKey, new ServiceIdentifier("other", typeof(IClock)));
    }

    /// <summary>
    /// 没有登记 IGuidGenerator 时回落到简单实现, 不抛异常
    /// </summary>
    [Fact]
    public async Task DomainService_GuidGenerator_FallsBackToSimpleGenerator()
    {
        using ServiceProvider provider = await BuildProviderAsync();
        using IServiceScope scope = provider.CreateScope();

        ProbeDomainService domainService = scope.ServiceProvider.GetRequiredService<ProbeDomainService>();

        Assert.NotEqual(Guid.Empty, domainService.ReadGuid());
    }

    private static async Task<ServiceProvider> BuildProviderAsync()
    {
        ServiceCollection services = new();

        // 领域层连带依赖多租户模块, 它在服务注册阶段要读根配置; 基类的 Logger 需要日志工厂
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        await services.AddApplicationAsync<DomainTestModule>();

        return services.BuildServiceProvider();
    }

    [DependsOn(typeof(SnowDomainModule))]
    private sealed class DomainTestModule : SnowModule
    {
    }

    /// <summary>
    /// 探针领域服务: 只用来验证基类成员能取到服务
    /// </summary>
    [TransientService]
    public sealed class ProbeDomainService : DomainService
    {
        /// <summary>
        /// 构造探针领域服务
        /// </summary>
        /// <param name="lazyServiceProvider">惰性服务提供者</param>
        public ProbeDomainService(ISnowLazyServiceProvider lazyServiceProvider)
            : base(lazyServiceProvider)
        {
        }

        /// <summary>
        /// 读时钟
        /// </summary>
        /// <returns>时钟</returns>
        public IClock ReadClock()
        {
            return Clock;
        }

        /// <summary>
        /// 生成一个 Guid
        /// </summary>
        /// <returns>新的 Guid</returns>
        public Guid ReadGuid()
        {
            return GuidGenerator.Create();
        }

        /// <summary>
        /// 读日志
        /// </summary>
        /// <returns>日志</returns>
        public ILogger ReadLogger()
        {
            return Logger;
        }

        /// <summary>
        /// 读异步查询执行器
        /// </summary>
        /// <returns>异步查询执行器</returns>
        public Snow.Linq.IAsyncQueryableExecuter ReadAsyncExecuter()
        {
            return AsyncExecuter;
        }
    }
}
