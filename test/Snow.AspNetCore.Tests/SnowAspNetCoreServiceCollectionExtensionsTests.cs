using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Snow.AspNetCore.Security.Claims;
using Snow.DependencyInjection;
using Snow.Security;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// Web 宿主服务登记扩展: 取宿主环境(没有就返回空实现) 与 声明名映射(登记 IClaimsTransformation)
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

    /// <summary>
    /// TransformSnowClaims 只登记一个 IClaimsTransformation, 不改已有的声明也不挂中间件
    /// </summary>
    [Fact]
    public void TransformSnowClaims_RegistersClaimsTransformation()
    {
        ServiceCollection services = new();
        services.AddOptions();

        Assert.Same(services, services.TransformSnowClaims());

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<SnowClaimsTransformation>(provider.GetRequiredService<IClaimsTransformation>());
    }

    /// <summary>
    /// 六条标准声明各映射一份, 值 / 值类型 / 签发者都跟着原声明走
    /// </summary>
    [Fact]
    public async Task TransformAsync_MapsStandardClaims()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim("sub", "user-1", ClaimValueTypes.String, "issuer-a"),
            new Claim("role", "admin"),
            new Claim("email", "a@b.c"),
            new Claim("name", "jamen"),
            new Claim("family_name", "Jiang"),
            new Claim("given_name", "Jamen"),
        ], "Bearer"));

        ClaimsPrincipal transformed = await CreateTransformation().TransformAsync(principal);

        Assert.Equal("user-1", transformed.FindFirst(UserClaimTypes.UserId)?.Value);
        Assert.Equal("issuer-a", transformed.FindFirst(UserClaimTypes.UserId)?.Issuer);
        Assert.Equal("admin", transformed.FindFirst(UserClaimTypes.Role)?.Value);
        Assert.Equal("a@b.c", transformed.FindFirst(UserClaimTypes.Email)?.Value);
        Assert.Equal("jamen", transformed.FindFirst(UserClaimTypes.UserName)?.Value);
        Assert.Equal("Jiang", transformed.FindFirst(UserClaimTypes.SurName)?.Value);
        Assert.Equal("Jamen", transformed.FindFirst(UserClaimTypes.Name)?.Value);
    }

    /// <summary>
    /// 原声明一律保留: 映射是复制一份换名字, 不是替换 —— 两边都能读到
    /// </summary>
    [Fact]
    public async Task TransformAsync_KeepsOriginalClaims()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "user-1")], "Bearer"));

        ClaimsPrincipal transformed = await CreateTransformation().TransformAsync(principal);

        Assert.Equal("user-1", transformed.FindFirst("sub")?.Value);
        Assert.Equal("user-1", transformed.FindFirst(UserClaimTypes.UserId)?.Value);
    }

    /// <summary>
    /// 没有一条命中映射表时不加空身份 —— 身份数量保持不变
    /// </summary>
    [Fact]
    public async Task TransformAsync_WithoutMatchedClaims_AddsNothing()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("custom", "x")], "Bearer"));

        ClaimsPrincipal transformed = await CreateTransformation().TransformAsync(principal);

        Assert.Single(transformed.Identities);
        Assert.Null(transformed.FindFirst(UserClaimTypes.UserId));
    }

    /// <summary>
    /// 宿主改过映射表就按改后的走 —— 值是委托, 读的是 UserClaimTypes 当时的属性值
    /// </summary>
    [Fact]
    public async Task TransformAsync_UsesConfiguredMap()
    {
        SnowClaimsMapOptions options = new();
        options.Maps["custom"] = () => UserClaimTypes.TenantId;
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("custom", "tenant-9")], "Bearer"));

        ClaimsPrincipal transformed = await new SnowClaimsTransformation(
            Options.Create(options)).TransformAsync(principal);

        Assert.Equal("tenant-9", transformed.FindFirst(UserClaimTypes.TenantId)?.Value);
    }

    private static SnowClaimsTransformation CreateTransformation()
        => new(Options.Create(new SnowClaimsMapOptions()));

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
