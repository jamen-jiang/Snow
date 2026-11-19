using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 模块化的服务提供者扩展:在容器构建完成后驱动模块的初始化与关闭
/// </summary>
public static class ModularityApplicationExtensions
{
    /// <summary>
    /// 驱动所有模块的应用初始化阶段
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    /// <returns>模块化应用实例</returns>
    public static RongiaApplication Initialize(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        RongiaApplication application = serviceProvider.GetRequiredService<RongiaApplication>();
        application.Initialize(serviceProvider);

        return application;
    }

    /// <summary>
    /// 驱动所有模块的应用初始化阶段(异步)
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    /// <returns>模块化应用实例</returns>
    public static async Task<RongiaApplication> InitializeAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        RongiaApplication application = serviceProvider.GetRequiredService<RongiaApplication>();
        await application.InitializeAsync(serviceProvider);

        return application;
    }

    /// <summary>
    /// 驱动所有模块的关闭阶段
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>模块化应用实例</returns>
    public static RongiaApplication Shutdown(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        RongiaApplication application = serviceProvider.GetRequiredService<RongiaApplication>();
        application.Shutdown();

        return application;
    }

    /// <summary>
    /// 驱动所有模块的关闭阶段(异步)
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>模块化应用实例</returns>
    public static async Task<RongiaApplication> ShutdownAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        RongiaApplication application = serviceProvider.GetRequiredService<RongiaApplication>();
        await application.ShutdownAsync();

        return application;
    }
}
