using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 应用初始化上下文扩展: 取不到就抛 / 取不到返回 null 两个版本
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
