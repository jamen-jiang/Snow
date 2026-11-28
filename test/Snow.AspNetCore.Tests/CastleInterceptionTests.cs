using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.Castle;
using Snow.DependencyInjection;
using Snow.DynamicProxy;
using Snow.Modularity;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 默认微软 DI 容器下的 Castle 拦截: 约定注册直接登记代理工厂。
/// <para>覆盖类代理、接口代理, 以及代理的生命周期(singleton 只造一次)。</para>
/// </summary>
public class CastleInterceptionTests
{
    /// <summary>
    /// 类代理: 非密封类 + virtual 方法, 按具体类型解析也能被拦到
    /// </summary>
    [Fact]
    public async Task ClassProxy_IsIntercepted()
    {
        CastleCountInterceptor.Reset();
        IServiceProvider provider = await BuildProviderAsync();

        try
        {
            CastleClassProbe probe = provider.GetRequiredService<CastleClassProbe>();
            string result = probe.Work("YG");

            Assert.Equal("work-YG", result);
            Assert.Equal(1, CastleCountInterceptor.Entered);
            Assert.Equal(1, CastleCountInterceptor.Exited);
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// 接口代理: 密封类只能接口代理, 按接口解析被拦到
    /// </summary>
    [Fact]
    public async Task InterfaceProxy_IsIntercepted()
    {
        CastleCountInterceptor.Reset();
        IServiceProvider provider = await BuildProviderAsync();

        try
        {
            ICastleSealedProbe probe = provider.GetRequiredService<ICastleSealedProbe>();
            string result = probe.Work("YG");

            Assert.Equal("sealed-YG", result);
            Assert.Equal(1, CastleCountInterceptor.Entered);
            Assert.Equal(1, CastleCountInterceptor.Exited);
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// singleton 代理只造一次: 多次解析拿到同一个对象
    /// </summary>
    [Fact]
    public async Task SingletonProxy_ResolvesSameInstanceAndKeepsLifetime()
    {
        CastleCountInterceptor.Reset();
        IServiceProvider provider = await BuildProviderAsync();

        try
        {
            Assert.IsType<Microsoft.Extensions.DependencyInjection.ServiceProvider>(provider);

            ICastleSingletonProbe first = provider.GetRequiredService<ICastleSingletonProbe>();
            ICastleSingletonProbe second = provider.GetRequiredService<ICastleSingletonProbe>();

            Assert.Same(first, second);
            Assert.NotEqual(typeof(CastleSingletonProbe), first.GetType());
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }

    private static async Task<IServiceProvider> BuildProviderAsync()
    {
        ServiceCollection services = new();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        await services.AddApplicationAsync<CastleAopTestModule>();

        return services.BuildServiceProviderFromFactory();
    }

    /// <summary>
    /// 测试模块: 挂 Castle 代理工厂, 并挂自己的拦截器登记器
    /// </summary>
    [DependsOn(typeof(SnowCastleCoreModule))]
    private sealed class CastleAopTestModule : SnowModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.Services.OnRegistered(RegisterInterceptors);
        }

        private static void RegisterInterceptors(IOnServiceRegistredContext context)
        {
            if (context.ImplementationType.IsDefined(typeof(CastleCountInterceptorAttribute), true))
            {
                context.Interceptors.TryAdd<CastleCountInterceptor>();
            }
        }
    }

    /// <summary>
    /// 计数标记: 只是元数据
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CastleCountInterceptorAttribute : Attribute
    {
    }

    /// <summary>
    /// 计数拦截器
    /// </summary>
    [TransientService]
    public sealed class CastleCountInterceptor : SnowInterceptor
    {
        /// <summary>进入次数</summary>
        public static int Entered;

        /// <summary>退出次数</summary>
        public static int Exited;

        /// <summary>清零计数</summary>
        public static void Reset()
        {
            Entered = 0;
            Exited = 0;
        }

        /// <inheritdoc />
        public override async Task InterceptAsync(ISnowMethodInvocation invocation)
        {
            Interlocked.Increment(ref Entered);

            await invocation.ProceedAsync();

            Interlocked.Increment(ref Exited);
        }
    }

    /// <summary>
    /// 类代理探针: 非密封 + virtual 方法
    /// </summary>
    [TransientService]
    [CastleCountInterceptor]
    public class CastleClassProbe
    {
        /// <summary>工作方法</summary>
        public virtual string Work(string name)
        {
            return "work-" + name;
        }
    }

    /// <summary>
    /// 接口代理探针的契约
    /// </summary>
    public interface ICastleSealedProbe
    {
        /// <summary>工作方法</summary>
        string Work(string name);
    }

    /// <summary>
    /// 密封实现: 只能接口代理
    /// </summary>
    [TransientService]
    [ExposeServices(typeof(ICastleSealedProbe))]
    [CastleCountInterceptor]
    public sealed class CastleSealedProbe : ICastleSealedProbe
    {
        /// <inheritdoc />
        public string Work(string name)
        {
            return "sealed-" + name;
        }
    }

    /// <summary>
    /// singleton 探针的契约
    /// </summary>
    public interface ICastleSingletonProbe
    {
        /// <summary>工作方法</summary>
        string Work(string name);
    }

    /// <summary>
    /// singleton 实现
    /// </summary>
    [SingletonService]
    [ExposeServices(typeof(ICastleSingletonProbe))]
    [CastleCountInterceptor]
    public sealed class CastleSingletonProbe : ICastleSingletonProbe
    {
        /// <inheritdoc />
        public string Work(string name)
        {
            return name;
        }
    }
}
