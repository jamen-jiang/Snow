using Snow.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// OpenAPI 的服务登记入口
/// </summary>
public static class SnowOpenApiServiceCollectionExtensions
{
    /// <summary>
    /// 调整 OpenAPI 选项。必须在 <c>AddApplication</c> 之前调用
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowOpenApi(this IServiceCollection services, Action<SnowOpenApiOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.PostConfigure(configure);
        return services;
    }
}
