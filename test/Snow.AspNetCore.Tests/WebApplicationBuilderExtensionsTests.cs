using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow;
using Snow.DependencyInjection;
using Snow.Modularity;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// Web 宿主装配入口: 对应 ABP 的 WebApplicationBuilderExtensions
/// <para>测的是它比 <c>builder.Services.AddApplicationAsync(...)</c> 多出来的两件事 ——
/// 配置顶进容器、环境名抄进创建选项。</para>
/// </summary>
public class WebApplicationBuilderExtensionsTests
{
    [Fact]
    public async Task AddApplicationAsync_ReplacesConfiguration()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Probe:Value"] = "from-builder",
        });

        await builder.AddApplicationAsync<EmptyModule>();

        IConfiguration configuration = builder.Services.GetSingletonInstance<IConfiguration>();
        Assert.Equal("from-builder", configuration["Probe:Value"]);
    }

    [Fact]
    public async Task AddApplicationAsync_TakesEnvironmentNameFromBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Staging";

        await builder.AddApplicationAsync<EmptyModule>();

        Assert.Equal("Staging", builder.Services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName);
    }

    [Fact]
    public async Task AddApplicationAsync_ExplicitEnvironmentWinsOverBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Staging";

        await builder.AddApplicationAsync<EmptyModule>(options => options.Environment = "Development");

        Assert.Equal("Development", builder.Services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName);
    }

    [Fact]
    public async Task AddApplicationAsync_ReturnsApplicationInstance()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        SnowApplication application = await builder.AddApplicationAsync<EmptyModule>();

        Assert.Same(application, builder.Services.GetSingletonInstance<SnowApplication>());
        Assert.NotEmpty(application.Modules);
    }

    [Fact]
    public async Task AddApplicationAsync_ByType_AlsoWorks()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        await builder.AddApplicationAsync(typeof(EmptyModule));

        Assert.Equal(
            builder.Environment.EnvironmentName,
            builder.Services.GetSingletonInstance<ISnowHostEnvironment>().EnvironmentName);
    }

    [Fact]
    public async Task AddApplicationAsync_ModuleSeesConfigurationDuringConfigureServices()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Probe:Value"] = "visible",
        });

        await builder.AddApplicationAsync<ConfigurationProbeModule>();

        Assert.Equal("visible", ConfigurationProbeModule.SeenValue);
    }

    private sealed class EmptyModule : SnowModule
    {
    }

    private sealed class ConfigurationProbeModule : SnowModule
    {
        public static string? SeenValue { get; private set; }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            SeenValue = context.Configuration["Probe:Value"];
        }
    }
}
