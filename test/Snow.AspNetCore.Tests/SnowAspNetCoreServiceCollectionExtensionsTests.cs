using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Snow;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// Web 宿主服务登记扩展: 取宿主环境(没有就返回空实现)
/// </summary>
public class SnowAspNetCoreServiceCollectionExtensionsTests
{
    /// <summary>
    /// 服务集合里登记了真实 Web 环境时原样返回它, 不包装
    /// </summary>
    [Fact]
    public void GetHostingEnvironment_ReturnsRegisteredEnvironment()
    {
        ServiceCollection services = new();
        FakeWebHostEnvironment environment = new("Staging");
        services.AddSingleton<IWebHostEnvironment>(environment);

        Assert.Same(environment, services.GetHostingEnvironment());
    }

    /// <summary>
    /// 没登记 Web 环境(控制台 / Worker / 还没被 Web 主机登记)时返回空实现, 环境名按 Development —— 调用方不必写 ?? 分支
    /// </summary>
    [Fact]
    public void GetHostingEnvironment_WithoutRegistration_ReturnsEmptyImplementation()
    {
        ServiceCollection services = new();

        IWebHostEnvironment environment = services.GetHostingEnvironment();

        // 与 ABP 的 EmptyHostingEnvironment 一样只给环境名, 其余属性留 null: 它只用来读环境名, 不能当真实环境用
        Assert.Equal(SnowEnvironments.Development, environment.EnvironmentName);
        Assert.Null(environment.WebRootPath);
        Assert.Null(environment.WebRootFileProvider);
    }

    /// <summary>
    /// 空实现每次都是新对象, 不会被当成真实环境在多次调用之间共享状态
    /// </summary>
    [Fact]
    public void GetHostingEnvironment_WithoutRegistration_ReturnsFreshInstanceEachTime()
    {
        ServiceCollection services = new();

        Assert.NotSame(services.GetHostingEnvironment(), services.GetHostingEnvironment());
    }

    private sealed class FakeWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = nameof(Snow.AspNetCore.Tests);

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
