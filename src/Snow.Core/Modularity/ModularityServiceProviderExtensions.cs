using Microsoft.Extensions.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 非 Web 宿主的初始化与关停入口:Web 宿主不用调,<c>SnowAspNetCoreModule</c> 注册的启动过滤器会自动驱动同样的阶段
/// </summary>
public static class ModularityServiceProviderExtensions
{
    /// <summary>
    /// 驱动全部模块的应用初始化阶段(预初始化 → 第一段 → 收尾段)
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    /// <returns>表示初始化过程的任务</returns>
    public static Task InitializeAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider.GetRequiredService<SnowApplication>().InitializeAsync(serviceProvider);
    }

    /// <summary>
    /// 驱动全部模块的应用初始化阶段(同步版本)
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    public static void Initialize(this IServiceProvider serviceProvider)
    {
        serviceProvider.InitializeAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 关停全部模块
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    /// <returns>表示关停过程的任务</returns>
    public static Task ShutdownAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider.GetRequiredService<SnowApplication>().ShutdownAsync();
    }

    /// <summary>
    /// 关停全部模块(同步版本)
    /// </summary>
    /// <param name="serviceProvider">已构建完成的服务提供者</param>
    public static void Shutdown(this IServiceProvider serviceProvider)
    {
        serviceProvider.ShutdownAsync().GetAwaiter().GetResult();
    }
}
