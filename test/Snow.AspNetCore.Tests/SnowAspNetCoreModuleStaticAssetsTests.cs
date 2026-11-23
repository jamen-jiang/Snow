using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Snow.Modularity;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 静态资源合并: 开关打开才并进 WebRoot, 关掉时把已登记的提供器明确打出来, 不静默丢弃
/// </summary>
public class SnowAspNetCoreModuleStaticAssetsTests
{
    /// <summary>
    /// 开关默认打开时登记的提供器并进 WebRoot, 宿主自己的提供器排在最前(同名文件宿主优先)
    /// </summary>
    [Fact]
    public void MergeStaticAssets_Enabled_ComposesWithHostProviderFirst()
    {
        NullFileProvider hostProvider = new();
        FakeWebHostEnvironment environment = new(hostProvider);
        NullFileProvider packageProvider = new();
        List<string> warnings = [];
        using ServiceProvider services = BuildServices(environment, options => options.StaticAssetFileProviders.Add(packageProvider), warnings);

        new SnowAspNetCoreModule().OnApplicationInitialization(new ApplicationInitializationContext(services));

        CompositeFileProvider composite = Assert.IsType<CompositeFileProvider>(environment.WebRootFileProvider);
        Assert.Equal([hostProvider, packageProvider], composite.FileProviders);
        Assert.Empty(warnings);
    }

    /// <summary>
    /// 合并关掉时已登记的提供器一个都不用, 但必须留下一条警告, 不能静默丢弃
    /// </summary>
    [Fact]
    public void MergeStaticAssets_Disabled_WarnsAndKeepsWebRootUntouched()
    {
        NullFileProvider hostProvider = new();
        FakeWebHostEnvironment environment = new(hostProvider);
        List<string> warnings = [];
        using ServiceProvider services = BuildServices(
            environment,
            options =>
            {
                options.MergeStaticAssets = false;
                options.StaticAssetFileProviders.Add(new NullFileProvider());
            },
            warnings);

        new SnowAspNetCoreModule().OnApplicationInitialization(new ApplicationInitializationContext(services));

        Assert.Same(hostProvider, environment.WebRootFileProvider);
        string warning = Assert.Single(warnings);
        Assert.Contains("MergeStaticAssets=false", warning);
        Assert.Contains("全部跳过", warning);
        Assert.Contains(nameof(NullFileProvider), warning);
    }

    /// <summary>
    /// 什么都没登记时关掉开关不算跳过, 不该打警告
    /// </summary>
    [Fact]
    public void MergeStaticAssets_DisabledWithoutRegistration_StaysQuiet()
    {
        NullFileProvider hostProvider = new();
        FakeWebHostEnvironment environment = new(hostProvider);
        List<string> warnings = [];
        using ServiceProvider services = BuildServices(environment, options => options.MergeStaticAssets = false, warnings);

        new SnowAspNetCoreModule().OnApplicationInitialization(new ApplicationInitializationContext(services));

        Assert.Empty(warnings);
        Assert.Same(hostProvider, environment.WebRootFileProvider);
    }

    /// <summary>
    /// 拿不到 Web 宿主环境(非 Web 宿主)时不能静默丢, 但告警口径要与「宿主主动关掉合并」分开
    /// </summary>
    [Fact]
    public void MergeStaticAssets_WithoutWebHostEnvironment_WarnsSeparately()
    {
        List<string> warnings = [];
        ServiceCollection services = new();
        services.AddSingleton<ILogger<SnowAspNetCoreModule>>(new WarningCollector(warnings));
        services.Configure<SnowAspNetCoreOptions>(options => options.StaticAssetFileProviders.Add(new NullFileProvider()));
        using ServiceProvider provider = services.BuildServiceProvider();

        new SnowAspNetCoreModule().OnApplicationInitialization(new ApplicationInitializationContext(provider));

        string warning = Assert.Single(warnings);
        Assert.Contains("不是 Web 应用", warning);
        Assert.Contains("1 个文件提供器", warning);
        Assert.DoesNotContain("MergeStaticAssets=false", warning);
    }

    private static ServiceProvider BuildServices(IWebHostEnvironment environment, Action<SnowAspNetCoreOptions> configure, List<string> warnings)
    {
        ServiceCollection services = new();
        services.AddSingleton(environment);
        services.AddSingleton<ILogger<SnowAspNetCoreModule>>(new WarningCollector(warnings));
        services.Configure(configure);
        return services.BuildServiceProvider();
    }

    private sealed class FakeWebHostEnvironment(IFileProvider webRoot) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Snow.AspNetCore.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = webRoot;

        public string WebRootPath { get; set; } = "wwwroot";

        public string EnvironmentName { get; set; } = "Development";

        public string ContentRootPath { get; set; } = ".";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class WarningCollector(List<string> warnings) : ILogger<SnowAspNetCoreModule>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
            {
                warnings.Add(formatter(state, exception));
            }
        }
    }
}
