using Microsoft.Extensions.DependencyInjection;
using Snow.ObjectMapping.Mapster;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Mapster 映射的服务登记入口
/// </summary>
public static class SnowObjectMappingMapsterServiceCollectionExtensions
{
    /// <summary>
    /// 调整 Mapster 映射选项。走标准 Options 管道(PostConfigure), 启动时结算
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowObjectMapping(this IServiceCollection services, Action<SnowObjectMappingMapsterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.PostConfigure(configure);
        return services;
    }
}
