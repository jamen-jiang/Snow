using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Snow.DependencyInjection;
using Snow.Guids;
using Snow.Linq;
using Snow.MultiTenancy;
using Snow.Timing;

namespace Snow.Domain.Services;

/// <summary>
/// 领域服务基类(对应 ABP 的 <c>DomainService</c>): 把领域服务里最常用的几项依赖做成属性, 子类不必逐个构造注入
/// <para>ABP 靠 Autofac 的属性注入把 <see cref="LazyServiceProvider"/> 填进来; Snow 没有属性注入,
/// 改由构造器传入, 语义一致: 属性第一次被读取时才向容器要实例, 用不到的依赖不产生解析开销。</para>
/// </summary>
public abstract class DomainService : IDomainService
{
    /// <summary>
    /// 惰性服务提供者: 属性依赖都从这里取
    /// </summary>
    public ISnowLazyServiceProvider LazyServiceProvider { get; }

    /// <summary>
    /// 构造领域服务基类
    /// </summary>
    /// <param name="lazyServiceProvider">惰性服务提供者</param>
    protected DomainService(ISnowLazyServiceProvider lazyServiceProvider)
    {
        LazyServiceProvider = lazyServiceProvider;
    }

    /// <summary>
    /// 时钟
    /// </summary>
    protected IClock Clock => LazyServiceProvider.LazyGetRequiredService<IClock>();

    /// <summary>
    /// Guid 生成器; 容器里没有登记时回落到 <see cref="SimpleGuidGenerator.Instance"/>
    /// </summary>
    protected IGuidGenerator GuidGenerator => LazyServiceProvider.LazyGetService<IGuidGenerator>(SimpleGuidGenerator.Instance);

    /// <summary>
    /// 日志工厂
    /// </summary>
    protected ILoggerFactory LoggerFactory => LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    /// <summary>
    /// 当前租户
    /// </summary>
    protected ICurrentTenant CurrentTenant => LazyServiceProvider.LazyGetRequiredService<ICurrentTenant>();

    /// <summary>
    /// 异步查询执行器: 把 IQueryable 交给真正的异步实现(EF Core)执行
    /// </summary>
    protected IAsyncQueryableExecuter AsyncExecuter => LazyServiceProvider.LazyGetRequiredService<IAsyncQueryableExecuter>();

    /// <summary>
    /// 日志; 取不到日志工厂时回落到 <see cref="NullLogger"/>
    /// </summary>
    protected ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(
        provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
}
