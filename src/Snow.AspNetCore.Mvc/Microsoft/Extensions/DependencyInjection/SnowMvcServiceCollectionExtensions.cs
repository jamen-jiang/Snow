using Microsoft.Extensions.DependencyInjection;
using Snow.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// MVC 与自动 API 的服务登记入口
/// </summary>
public static class SnowMvcServiceCollectionExtensions
{
    /// <summary>
    /// 调整自动 API 选项
    /// <para>走标准 Options 管道(PostConfigure), 启动时结算, 后登记者覆盖先登记者; 一般在本模块之前调用。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowMvc(this IServiceCollection services, Action<SnowAspNetCoreMvcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.PostConfigure(configure);
        return services;
    }
}
