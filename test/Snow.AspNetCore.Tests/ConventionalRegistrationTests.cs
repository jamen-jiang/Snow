using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.Castle;

using ServiceProvider = Snow.AspNetCore.Tests.TestServiceProvider;
using Snow.DependencyInjection;
using Snow.DynamicProxy;
using Snow.Modularity;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 约定注册的回归用例: 一个实现类型暴露多个服务类型时只产出一个实例、internal 实现类型不被丢弃、
/// 宿主显式登记过的实现类型挂拦截器时不会变成两套对象、接口代理的 target 能被容器释放。
/// </summary>
public class ConventionalRegistrationTests
{
    /// <summary>
    /// 单例实现同时暴露两个接口: 两个接口解析到同一个对象
    /// </summary>
    [Fact]
    public async Task Singleton_ExposingTwoInterfaces_ResolvesSameInstance()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        INotifier notifier = provider.GetRequiredService<INotifier>();
        ISubscriber subscriber = provider.GetRequiredService<ISubscriber>();

        Assert.Same(notifier, subscriber);
        Assert.Same(notifier, provider.GetRequiredService<INotifier>());
    }

    /// <summary>
    /// internal 的实现类型也要被约定注册(不能因为不可见就静默丢弃)
    /// </summary>
    [Fact]
    public async Task InternalImplementation_IsRegistered()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        Assert.NotNull(provider.GetService<IInternalWorker>());
    }

    /// <summary>
    /// 宿主显式登记了实现类型, 类型上又挂了拦截器: 按具体类型与按接口解析必须拿到同一个被代理对象
    /// </summary>
    [Fact]
    public async Task ExplicitlyRegisteredImplementation_WithInterceptor_SharesProxiedInstance()
    {
        CountingProbeInterceptor.Reset();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        // 宿主自己先登记一次具体类型(常见写法), 然后再跑约定注册
        services.AddTransient<AuditService>();

        await services.AddApplicationAsync<RegistrationTestModule>();

        using ServiceProvider provider = new ServiceProvider(services.BuildServiceProviderFromFactory());

        using (IServiceScope scope = provider.CreateScope())
        {
            AuditService concrete = scope.ServiceProvider.GetRequiredService<AuditService>();
            IAuditService byInterface = scope.ServiceProvider.GetRequiredService<IAuditService>();

            Assert.Same(concrete, byInterface);

            Assert.Equal(7, concrete.Ping());
            Assert.Equal(1, CountingProbeInterceptor.Entered);
        }
    }

    /// <summary>
    /// 接口代理的 target 由工厂手工建出: 作用域结束时容器要能释放它
    /// </summary>
    [Fact]
    public async Task InterfaceProxy_DisposesTargetWithScope()
    {
        DisposableProbe.DisposeCount = 0;

        using ServiceProvider provider = await BuildProviderAsync();

        using (IServiceScope scope = provider.CreateScope())
        {
            IDisposableProbe probe = scope.ServiceProvider.GetRequiredService<IDisposableProbe>();
            probe.Run();
        }

        Assert.Equal(1, DisposableProbe.DisposeCount);
    }

    private static async Task<ServiceProvider> BuildProviderAsync()
    {
        ServiceCollection services = new();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        await services.AddApplicationAsync<RegistrationTestModule>();

        return new ServiceProvider(services.BuildServiceProviderFromFactory());
    }

    /// <summary>
    /// 测试模块: 按标记给实现类型挂上计数拦截器
    /// </summary>
    [DependsOn(typeof(SnowCastleCoreModule))]

    private sealed class RegistrationTestModule : SnowModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.Services.OnRegistered(RegisterInterceptors);
        }

        private static void RegisterInterceptors(IOnServiceRegistredContext context)
        {
            if (context.ImplementationType.IsDefined(typeof(RegistrationInterceptAttribute), true))
            {
                context.Interceptors.TryAdd<CountingProbeInterceptor>();
            }
        }
    }

    /// <summary>
    /// 通知契约
    /// </summary>
    public interface INotifier
    {
        /// <summary>
        /// 发送通知
        /// </summary>
        /// <param name="message">消息</param>
        void Notify(string message);
    }

    /// <summary>
    /// 订阅契约
    /// </summary>
    public interface ISubscriber
    {
        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="topic">主题</param>
        void Subscribe(string topic);
    }

    /// <summary>
    /// 同时实现两个接口的单例实现
    /// </summary>
    [SingletonService]
    [ExposeServices(typeof(INotifier), typeof(ISubscriber))]
    public sealed class Notifier : INotifier, ISubscriber
    {
        /// <inheritdoc />
        public void Notify(string message)
        {
        }

        /// <inheritdoc />
        public void Subscribe(string topic)
        {
        }
    }

    /// <summary>
    /// 内部实现契约
    /// </summary>
    public interface IInternalWorker
    {
        /// <summary>
        /// 干活
        /// </summary>
        void Work();
    }

    /// <summary>
    /// internal 的实现类型: 不可见, 但必须照常登记
    /// </summary>
    [TransientService]
    [ExposeServices(typeof(IInternalWorker))]
    internal sealed class InternalWorker : IInternalWorker
    {
        /// <inheritdoc />
        public void Work()
        {
        }
    }

    /// <summary>
    /// 审计契约
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// 探针方法
        /// </summary>
        /// <returns>固定返回值</returns>
        int Ping();
    }

    /// <summary>
    /// 非密封的实现类型: 有公开虚方法, 可以做类代理
    /// </summary>
    [ScopedService]
    [RegistrationIntercept]
    public class AuditService : IAuditService
    {
        /// <inheritdoc />
        public virtual int Ping()
        {
            return 7;
        }
    }

    /// <summary>
    /// 可释放契约
    /// </summary>
    public interface IDisposableProbe
    {
        /// <summary>
        /// 执行
        /// </summary>
        void Run();
    }

    /// <summary>
    /// 密封实现, 只能走接口代理: 代理必须把 Dispose 转发进来
    /// </summary>
    [TransientService]
    [ExposeServices(typeof(IDisposableProbe))]
    [RegistrationIntercept]
    public sealed class DisposableProbe : IDisposableProbe, IDisposable
    {
        /// <summary>
        /// 被释放的次数
        /// </summary>
        public static int DisposeCount;

        /// <inheritdoc />
        public void Run()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            DisposeCount++;
        }
    }

    /// <summary>
    /// 计数标记: 只是元数据
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RegistrationInterceptAttribute : Attribute
    {
    }

    /// <summary>
    /// 计数拦截器
    /// </summary>
    [TransientService]
    public sealed class CountingProbeInterceptor : SnowInterceptor
    {
        /// <summary>
        /// 进入次数
        /// </summary>
        public static int Entered;

        /// <summary>
        /// 清零
        /// </summary>
        public static void Reset()
        {
            Entered = 0;
        }

        /// <inheritdoc />
        /// <param name="invocation">方法调用</param>
        public override async Task InterceptAsync(ISnowMethodInvocation invocation)
        {
            Interlocked.Increment(ref Entered);

            await invocation.ProceedAsync();
        }
    }
}
