using Microsoft.Extensions.FileProviders;

namespace Snow.AspNetCore;

/// <summary>
/// Web 宿主的服务登记扩展
/// </summary>
public static class SnowAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个额外静态资源提供器(包内 wwwroot、嵌入式资源等), 启动时合并进 WebRoot
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="fileProvider">文件提供器</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowStaticAssets(this IServiceCollection services, IFileProvider fileProvider)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(fileProvider);

        services.Configure<SnowAspNetCoreOptions>(options => options.StaticAssetFileProviders.Add(fileProvider));

        return services;
    }
}
