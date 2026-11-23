using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;
using Snow.Modularity;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 应用初始化上下文扩展: 每个宿主对象都有"取不到就抛"与"取不到返回 null"两个版本
/// </summary>
public class ApplicationInitializationContextExtensionsTests
{
    /// <summary>
    /// 应用构建器: 访问器填过就返回同一个实例
    /// </summary>
    [Fact]
    public void GetApplicationBuilder_ReturnsRegisteredBuilder()
    {
        using WebApplication app = WebApplication.CreateBuilder().Build();
        ApplicationInitializationContext context = CreateContext(services => services.AddObjectAccessor<IApplicationBuilder>(app));

        Assert.Same(app, context.GetApplicationBuilder());
        Assert.Same(app, context.GetApplicationBuilderOrNull());
    }

    /// <summary>
    /// 应用构建器: 没填访问器时, 不带的版本抛初始化异常, 带的版本返回 null(非 Web 宿主走这条)
    /// </summary>
    [Fact]
    public void GetApplicationBuilder_WhenMissing_ThrowsAndOrNullReturnsNull()
    {
        ApplicationInitializationContext context = CreateContext();

        Assert.Null(context.GetApplicationBuilderOrNull());
        Assert.Throws<SnowInitializationException>(context.GetApplicationBuilder);
    }

    /// <summary>
    /// 宿主: 没有访问器时回落到容器里的 IHost
    /// </summary>
    [Fact]
    public void GetHost_FallsBackToContainerHost()
    {
        using WebApplication app = WebApplication.CreateBuilder().Build();
        ApplicationInitializationContext context = CreateContext(services => services.AddSingleton<IHost>(app));

        Assert.Same(app, context.GetHost());
        Assert.Same(app, context.GetHostOrNull());
    }

    /// <summary>
    /// 宿主: 访问器与容器都没有时, 不带的版本抛初始化异常
    /// </summary>
    [Fact]
    public void GetHost_WhenMissing_Throws()
    {
        ApplicationInitializationContext context = CreateContext();

        Assert.Null(context.GetHostOrNull());
        Assert.Throws<SnowInitializationException>(context.GetHost);
    }

    /// <summary>
    /// 端点路由构建器: 访问器填过就返回; 没填时不带的版本抛初始化异常
    /// </summary>
    [Fact]
    public void GetEndpointRouteBuilder_ReturnsRegisteredBuilder()
    {
        using WebApplication app = WebApplication.CreateBuilder().Build();
        ApplicationInitializationContext filled = CreateContext(services => services.AddObjectAccessor<IEndpointRouteBuilder>(app));
        ApplicationInitializationContext empty = CreateContext();

        Assert.Same(app, filled.GetEndpointRouteBuilder());
        Assert.Same(app, filled.GetEndpointRouteBuilderOrNull());
        Assert.Null(empty.GetEndpointRouteBuilderOrNull());
        Assert.Throws<SnowInitializationException>(empty.GetEndpointRouteBuilder);
    }

    /// <summary>
    /// Web 应用: 通用主机下返回 null, 不带的版本抛初始化异常
    /// </summary>
    [Fact]
    public void GetWebApplication_WhenMissing_ThrowsAndOrNullReturnsNull()
    {
        using WebApplication app = WebApplication.CreateBuilder().Build();
        ApplicationInitializationContext filled = CreateContext(services => services.AddObjectAccessor<WebApplication>(app));
        ApplicationInitializationContext empty = CreateContext();

        Assert.Same(app, filled.GetWebApplication());
        Assert.Same(app, filled.GetWebApplicationOrNull());
        Assert.Null(empty.GetWebApplicationOrNull());
        Assert.Throws<SnowInitializationException>(empty.GetWebApplication);
    }

    /// <summary>
    /// 环境: 容器里有 IWebHostEnvironment 就能取到, 没有时带的版本返回 null
    /// </summary>
    [Fact]
    public void GetEnvironment_ReturnsHostEnvironment()
    {
        using WebApplication app = WebApplication.CreateBuilder().Build();
        ApplicationInitializationContext context = CreateContext(services => services.AddSingleton(app.Environment));

        Assert.Same(app.Environment, context.GetEnvironment());
        Assert.Same(app.Environment, context.GetEnvironmentOrNull());
        Assert.Null(CreateContext().GetEnvironmentOrNull());
    }

    /// <summary>
    /// 配置与日志工厂: 直接来自容器, 初始化阶段就有
    /// </summary>
    [Fact]
    public void GetConfigurationAndLoggerFactory_ComeFromContainer()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ApplicationInitializationContext context = CreateContext(services =>
        {
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
        });

        Assert.Same(configuration, context.GetConfiguration());
        Assert.NotNull(context.GetLoggerFactory());
    }

    /// <summary>
    /// 服务提供者访问器的扩展: 初始化上下文能直接取到当前请求, 不在请求线程上时返回 null
    /// </summary>
    [Fact]
    public void GetHttpContext_ReturnsCurrentHttpContext()
    {
        DefaultHttpContext httpContext = new();
        ApplicationInitializationContext context = CreateContext(services =>
        {
            services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
        });

        Assert.Same(httpContext, context.GetHttpContext());

        // 另一个容器里没有 IHttpContextAccessor 时直接抛, 而不是悄悄返回 null
        Assert.Throws<InvalidOperationException>(CreateContext().GetHttpContext);
    }

    /// <summary>
    /// 造一个应用初始化上下文: 容器里按需追加服务
    /// </summary>
    /// <param name="configure">追加服务的动作</param>
    /// <returns>应用初始化上下文</returns>
    private static ApplicationInitializationContext CreateContext(Action<IServiceCollection>? configure = null)
    {
        ServiceCollection services = new();
        configure?.Invoke(services);

        return new ApplicationInitializationContext(services.BuildServiceProvider());
    }
}
