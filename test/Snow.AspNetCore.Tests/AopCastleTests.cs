using System.Reflection;
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
/// AOP 代理链: 拦截器按 ABP 的方式登记(特性当元数据 + 登记器挂拦截器类), 经 Snow.DynamicProxy + Castle 适配层后生效。
/// <para>覆盖接口代理的同步/异步/泛型异步返回值、短路、实参可见性, 以及返回异步流时按次枚举。</para>
/// </summary>
public class AopCastleTests
{
    /// <summary>
    /// 接口代理拦到同步方法: 前置与后置都执行, 返回值原样透出
    /// </summary>
    [Fact]
    public async Task InterfaceProxy_InterceptsSyncMethod()
    {
        CountInterceptor.Reset();
        using ServiceProvider provider = await BuildProviderAsync();

        IGreeter greeter = provider.GetRequiredService<IGreeter>();
        string result = greeter.Greet("YG");

        Assert.Equal("hello YG", result);
        Assert.Equal(1, CountInterceptor.Entered);
        Assert.Equal(1, CountInterceptor.Exited);
        Assert.Equal("YG", CountInterceptor.LastArgument);
    }

    /// <summary>
    /// 返回 Task 的方法: 拦截链完整跑完才完成
    /// </summary>
    [Fact]
    public async Task InterfaceProxy_InterceptsAsyncMethod()
    {
        CountInterceptor.Reset();
        using ServiceProvider provider = await BuildProviderAsync();

        IGreeter greeter = provider.GetRequiredService<IGreeter>();
        await greeter.GreetAsync("YG");

        Assert.Equal(1, CountInterceptor.Entered);
        Assert.Equal(1, CountInterceptor.Exited);
    }

    /// <summary>
    /// 返回 Task&lt;T&gt; 的方法: 拦截后结果仍是目标方法的返回值
    /// </summary>
    [Fact]
    public async Task InterfaceProxy_PreservesGenericTaskResult()
    {
        CountInterceptor.Reset();
        using ServiceProvider provider = await BuildProviderAsync();

        IGreeter greeter = provider.GetRequiredService<IGreeter>();
        int length = await greeter.CountAsync("abcd");

        Assert.Equal(4, length);
        Assert.Equal(1, CountInterceptor.Entered);
    }

    /// <summary>
    /// 内层拦截器短路: 不调用下一环, 直接给出返回值
    /// </summary>
    [Fact]
    public async Task Interceptor_CanShortCircuit()
    {
        CountInterceptor.Reset();
        using ServiceProvider provider = await BuildProviderAsync();

        IGreeter greeter = provider.GetRequiredService<IGreeter>();
        string result = greeter.ShortCircuited();

        Assert.Equal(1, CountInterceptor.Entered);
        Assert.Equal(1, CountInterceptor.Exited);
        Assert.Equal("short-circuited", result);
    }

    /// <summary>
    /// 返回 IAsyncEnumerable 的方法: 每次枚举都被拦截器包住, 元素完整
    /// </summary>
    [Fact]
    public async Task InterfaceProxy_InterceptsAsyncEnumerable()
    {
        CountInterceptor.Reset();
        using ServiceProvider provider = await BuildProviderAsync();

        IGreeter greeter = provider.GetRequiredService<IGreeter>();
        List<int> items = new();

        await foreach (int item in greeter.RangeAsync(3))
        {
            items.Add(item);
        }

        Assert.Equal(new[] { 0, 1, 2 }, items);
        Assert.Equal(1, CountInterceptor.Entered);
        Assert.Equal(1, CountInterceptor.Exited);
    }

    private static async Task<ServiceProvider> BuildProviderAsync()
    {
        ServiceCollection services = new();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        await services.AddApplicationAsync<AopTestModule>();

        return new ServiceProvider(services.BuildServiceProviderFromFactory());
    }

    /// <summary>
    /// 测试模块: 挂上自己的拦截器登记器(与各业务模块的写法一致)
    /// </summary>
    [DependsOn(typeof(SnowCastleCoreModule))]

    private sealed class AopTestModule : SnowModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.Services.OnRegistered(RegisterInterceptors);
        }

        private static void RegisterInterceptors(IOnServiceRegistredContext context)
        {
            Type type = context.ImplementationType;

            if (type.IsDefined(typeof(CountInterceptorAttribute), true) || HasMethodAttribute<CountInterceptorAttribute>(type))
            {
                context.Interceptors.TryAdd<CountInterceptor>();
            }

            if (HasMethodAttribute<ShortCircuitInterceptorAttribute>(type))
            {
                context.Interceptors.TryAdd<ShortCircuitInterceptor>();
            }
        }

        private static bool HasMethodAttribute<TAttribute>(Type type) where TAttribute : Attribute
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.IsDefined(typeof(TAttribute), true))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 被代理的契约
    /// </summary>
    public interface IGreeter
    {
        /// <summary>
        /// 同步方法
        /// </summary>
        /// <param name="name">名字</param>
        /// <returns>问候语</returns>
        string Greet(string name);

        /// <summary>
        /// 返回 Task 的方法
        /// </summary>
        /// <param name="name">名字</param>
        /// <returns>表示执行过程的任务</returns>
        Task GreetAsync(string name);

        /// <summary>
        /// 返回 Task&lt;T&gt; 的方法
        /// </summary>
        /// <param name="text">文本</param>
        /// <returns>文本长度</returns>
        Task<int> CountAsync(string text);

        /// <summary>
        /// 被短路的方法
        /// </summary>
        /// <returns>短路后的返回值</returns>
        string ShortCircuited();

        /// <summary>
        /// 返回异步流的方法
        /// </summary>
        /// <param name="count">元素个数</param>
        /// <returns>从 0 开始的自增序列</returns>
        IAsyncEnumerable<int> RangeAsync(int count);
    }

    /// <summary>
    /// 实现类: 类级标记覆盖全类, 方法级标记只覆盖短路方法
    /// </summary>
    [TransientService]
    [ExposeServices(typeof(IGreeter))]
    [CountInterceptor]
    public sealed class Greeter : IGreeter
    {
        /// <inheritdoc />
        public string Greet(string name)
        {
            return "hello " + name;
        }

        /// <inheritdoc />
        public async Task GreetAsync(string name)
        {
            await Task.Yield();
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(string text)
        {
            await Task.Yield();
            return text.Length;
        }

        /// <inheritdoc />
        [ShortCircuitInterceptor]
        public string ShortCircuited()
        {
            return "should-not-be-called";
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<int> RangeAsync(int count)
        {
            for (int index = 0; index < count; index++)
            {
                await Task.Yield();
                yield return index;
            }
        }
    }

    /// <summary>
    /// 计数标记: 只是元数据
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class CountInterceptorAttribute : Attribute
    {
    }

    /// <summary>
    /// 计数拦截器: 记录进入/退出次数和第一个实参
    /// </summary>
    [TransientService]
    public sealed class CountInterceptor : SnowInterceptor
    {
        /// <summary>
        /// 进入次数
        /// </summary>
        public static int Entered;

        /// <summary>
        /// 退出次数
        /// </summary>
        public static int Exited;

        /// <summary>
        /// 最近一次的第一个实参
        /// </summary>
        public static string? LastArgument;

        /// <summary>
        /// 清零计数
        /// </summary>
        public static void Reset()
        {
            Entered = 0;
            Exited = 0;
            LastArgument = null;
        }

        /// <inheritdoc />
        /// <param name="invocation">方法调用</param>
        public override async Task InterceptAsync(ISnowMethodInvocation invocation)
        {
            Interlocked.Increment(ref Entered);
            LastArgument = invocation.Arguments.Length > 0 ? invocation.Arguments[0]?.ToString() : null;

            await invocation.ProceedAsync();

            Interlocked.Increment(ref Exited);
        }
    }

    /// <summary>
    /// 短路标记: 只是元数据
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ShortCircuitInterceptorAttribute : Attribute
    {
    }

    /// <summary>
    /// 短路拦截器: 直接给返回值, 不执行目标方法
    /// </summary>
    [TransientService]
    public sealed class ShortCircuitInterceptor : SnowInterceptor
    {
        /// <inheritdoc />
        /// <param name="invocation">方法调用</param>
        public override Task InterceptAsync(ISnowMethodInvocation invocation)
        {
            if (!invocation.Method.IsDefined(typeof(ShortCircuitInterceptorAttribute), true))
            {
                return invocation.ProceedAsync();
            }

            invocation.ReturnValue = "short-circuited";

            return Task.CompletedTask;
        }
    }
}
