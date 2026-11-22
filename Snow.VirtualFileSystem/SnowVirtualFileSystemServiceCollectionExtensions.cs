using Microsoft.Extensions.DependencyInjection;

namespace Snow.VirtualFileSystem;

/// <summary>
/// 虚拟文件系统的服务登记入口
/// </summary>
public static class SnowVirtualFileSystemServiceCollectionExtensions
{
    /// <summary>
    /// 登记文件集
    /// <para>选项在运行时解析, 所以只要在 <c>AddApplication</c> 之前调用即可, 与模块加载顺序无关。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">文件集配置动作</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowVirtualFileSystem(this IServiceCollection services, Action<SnowVirtualFileSystemOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services;
    }
}
