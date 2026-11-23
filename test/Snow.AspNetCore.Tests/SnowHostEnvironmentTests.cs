using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Snow.DependencyInjection;
using Snow.Modularity;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 宿主环境名: 框架在模块装配前放一个可写单例, Web 宿主模块把真实环境名填进去, 全程没人填则按生产环境处理
/// </summary>
public class SnowHostEnvironmentTests
{
    /// <summary>
    /// 默认实现只有环境名一个属性, 且初始为空 —— 空值由框架在服务注册收尾时兜底成 Production
    /// </summary>
    [Fact]
    public void SnowHostEnvironment_StartsWithNullName()
    {
        SnowHostEnvironment environment = new();

        Assert.Null(environment.EnvironmentName);
    }

    /// <summary>
    /// 四个判断扩展都忽略大小写, 与 Microsoft.Extensions.Hosting.Environments 的比较口径一致
    /// </summary>
    [Fact]
    public void Extensions_CompareIgnoringCase()
    {
        SnowHostEnvironment environment = new() { EnvironmentName = "development" };

        Assert.True(environment.IsDevelopment());
        Assert.False(environment.IsStaging());
        Assert.False(environment.IsProduction());
        Assert.True(environment.IsEnvironment("DEVELOPMENT"));
    }

    /// <summary>
    /// 环境名为空时四个判断全为 false, 不会因为 null 抛异常
    /// </summary>
    [Fact]
    public void Extensions_WithEmptyName_ReturnFalse()
    {
        SnowHostEnvironment environment = new();

        Assert.False(environment.IsDevelopment());
        Assert.False(environment.IsStaging());
        Assert.False(environment.IsProduction());
        Assert.False(environment.IsEnvironment(SnowEnvironments.Production));
    }

    /// <summary>
    /// Web 宿主模块在 PreConfigureServices 里把 IWebHostEnvironment 的环境名填进可写单例
    /// </summary>
    [Fact]
    public void PreConfigureServices_FillsNameFromWebHostEnvironment()
    {
        ServiceCollection services = new();
        services.AddSingleton<ISnowHostEnvironment>(new SnowHostEnvironment());
        services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment("Staging"));

        new SnowAspNetCoreModule().PreConfigureServices(new ServiceConfigurationContext(services));

        ISnowHostEnvironment hostEnvironment = services.GetSingletonInstance<ISnowHostEnvironment>();
        Assert.Equal("Staging", hostEnvironment.EnvironmentName);
    }

    /// <summary>
    /// 宿主自己已经填过环境名时模块不覆盖它
    /// </summary>
    [Fact]
    public void PreConfigureServices_KeepsHostProvidedName()
    {
        ServiceCollection services = new();
        services.AddSingleton<ISnowHostEnvironment>(new SnowHostEnvironment { EnvironmentName = "Production" });
        services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment("Development"));

        new SnowAspNetCoreModule().PreConfigureServices(new ServiceConfigurationContext(services));

        Assert.Equal("Production", services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName);
    }

    /// <summary>
    /// 没有 Web 宿主环境(非 Web 宿主)时 GetHostingEnvironment() 给出空实现, 环境名按 Development ——
    /// 与 ABP 的 AbpAspNetCoreModule 走同一条路, 不留下空环境名
    /// </summary>
    [Fact]
    public void PreConfigureServices_WithoutWebHostEnvironment_FallsBackToDevelopment()
    {
        ServiceCollection services = new();
        services.AddSingleton<ISnowHostEnvironment>(new SnowHostEnvironment());

        new SnowAspNetCoreModule().PreConfigureServices(new ServiceConfigurationContext(services));

        Assert.Equal(
            SnowEnvironments.Development,
            services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName);
    }

    /// <summary>
    /// AddApplication 在模块装配前就把可写单例放好: 模块在 PreConfigureServices 里能取到它(此时还没人填值),
    /// 填进去的名字也不会被收尾的兜底改掉
    /// </summary>
    [Fact]
    public void AddApplication_RegistersWritableEnvironmentBeforeModules()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddApplication<NameProbeModule>();

        Assert.True(NameProbeModule.SawAccessor);
        Assert.Equal("probed", services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName);
    }

    /// <summary>
    /// 全程没人填环境名时, 服务注册收尾把它兜底成 Production
    /// </summary>
    [Fact]
    public void ConfigureServices_FallsBackToProduction()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddApplication<EmptyModule>();

        Assert.Equal(
            SnowEnvironments.Production,
            services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName);
    }

    /// <summary>
    /// 模块自己填过环境名时兜底不改它
    /// </summary>
    [Fact]
    public void ConfigureServices_KeepsNameFilledByModule()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddApplication<NameFillingModule>();

        Assert.Equal("Staging", services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName);
    }

    private sealed class NameProbeModule : SnowModule
    {
        public static bool SawAccessor { get; private set; }

        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            SawAccessor = context.Services.GetSingletonInstanceOrNull<ISnowHostEnvironment>() is not null;
            context.Services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName = "probed";
        }
    }

    private sealed class EmptyModule : SnowModule { }

    private sealed class NameFillingModule : SnowModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName = "Staging";
        }
    }

    private sealed class FakeWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Snow.AspNetCore.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = "wwwroot";

        public string EnvironmentName { get; set; } = environmentName;

        public string ContentRootPath { get; set; } = ".";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
