using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// HTTP 管道: 管道由宿主手写, 端点由模块统一映射, 两段初始化按序执行
/// </summary>
public class SnowAspNetCorePipelineTests
{
    /// <summary>
    /// 端点映射执行各模块登记的动作, 且给的动作能拿到端点构建器
    /// </summary>
    [Fact]
    public void UseSnowEndpoints_RunsRegisteredActions()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();

        List<Type> seen = [];
        builder.Services.AddOptions<SnowEndpointRouterOptions>()
            .Configure(options => options.EndpointConfigureActions.Add(context => seen.Add(context.Endpoints.GetType())));

        WebApplication app = builder.Build();

        app.UseSnowEndpoints();

        Assert.Single(seen);
    }

    /// <summary>
    /// 宿主额外传进来的映射动作也会执行: 一个模块动作都没登记时同样生效(对应 ABP UseConfiguredEndpoints 的可选参数)
    /// </summary>
    [Fact]
    public void UseSnowEndpoints_RunsAdditionalConfigurationAction()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddOptions<SnowEndpointRouterOptions>();

        WebApplication app = builder.Build();

        bool invoked = false;
        app.UseSnowEndpoints(endpoints => invoked = endpoints is not null);

        Assert.True(invoked);
    }

    /// <summary>
    /// 既没登记动作也没传额外动作时原样返回, 一个中间件都不加
    /// </summary>
    [Fact]
    public void UseSnowEndpoints_WithoutAnyAction_DoesNothing()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddOptions<SnowEndpointRouterOptions>();

        WebApplication app = builder.Build();

        Assert.Same(app, app.UseSnowEndpoints());
    }

    /// <summary>
    /// 服务注册阶段是三趟: 全部模块的前段 → 全部模块的中段 → 全部模块的后段
    /// </summary>
    [Fact]
    public void ConfigureServices_RunsThreeStagesInOrder()
    {
        List<string> executed = [];

        PhaseTestModule.ConfigureRecorder = executed;

        try
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddSingleton(new List<string>());
            services.AddApplication<PhaseTestModule>();

            Assert.Equal(["pre-configure", "configure", "post-configure"], executed);
        }
        finally
        {
            PhaseTestModule.ConfigureRecorder = null;
        }
    }

    /// <summary>
    /// 一次跑完整个初始化: 预初始化 → 第一段 → 收尾段
    /// </summary>
    [Fact]
    public async Task Initialize_RunsAllPhasesInOrder()
    {
        List<string> executed = [];
        ServiceProvider provider = CreateProvider(executed);

        await provider.GetRequiredService<SnowApplication>().InitializeAsync(provider);

        Assert.Equal(["pre", "on", "post"], executed);
    }

    /// <summary>
    /// 关停阶段跑一次就够: 重复调用幂等, 且关停里能解析到依赖
    /// </summary>
    [Fact]
    public async Task Shutdown_RunsModulesOnce()
    {
        List<string> executed = [];
        ServiceProvider provider = CreateProvider(executed);
        SnowApplication application = provider.GetRequiredService<SnowApplication>();

        await application.InitializeAsync(provider);
        await application.ShutdownAsync();
        await application.ShutdownAsync();

        Assert.Equal(["pre", "on", "post", "shutdown"], executed);
    }

    /// <summary>
    /// 公开入口重复调用幂等: 三个阶段都只执行一次
    /// </summary>
    [Fact]
    public async Task Initialize_CalledTwice_RunsAllPhasesOnce()
    {
        List<string> executed = [];
        ServiceProvider provider = CreateProvider(executed);
        SnowApplication application = provider.GetRequiredService<SnowApplication>();

        await application.InitializeAsync(provider);
        await application.InitializeAsync(provider);

        Assert.Equal(["pre", "on", "post"], executed);
    }

    /// <summary>
    /// 造一个带日志与路由的容器: 骨架里的 UseRouting 才挂得上去
    /// </summary>
    /// <returns>根服务提供者</returns>
    private static ServiceProvider CreateServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddRouting();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 造一个跑完服务注册阶段的容器, 里面只有测试模块
    /// </summary>
    /// <param name="executed">模块记录执行顺序的列表</param>
    /// <returns>根服务提供者</returns>
    private static ServiceProvider CreateProvider(List<string> executed)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(executed);
        services.AddApplication<PhaseTestModule>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 只记录两段是否跑过的测试模块
    /// </summary>
    public sealed class PhaseTestModule : SnowModule
    {
        /// <summary>
        /// 服务注册三趟共用的记录表; 那三趟还没构建容器, 拿不到注入的服务
        /// </summary>
        public static List<string>? ConfigureRecorder { get; set; }

        /// <inheritdoc />
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            ConfigureRecorder?.Add("pre-configure");
        }

        /// <inheritdoc />
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            ConfigureRecorder?.Add("configure");
        }

        /// <inheritdoc />
        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            ConfigureRecorder?.Add("post-configure");
        }

        /// <inheritdoc />
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            context.ServiceProvider.GetRequiredService<List<string>>().Add("pre");
        }

        /// <inheritdoc />
        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            context.ServiceProvider.GetRequiredService<List<string>>().Add("shutdown");
        }

        /// <inheritdoc />
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            context.ServiceProvider.GetRequiredService<List<string>>().Add("on");
        }

        /// <inheritdoc />
        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            context.ServiceProvider.GetRequiredService<List<string>>().Add("post");
        }
    }
}
