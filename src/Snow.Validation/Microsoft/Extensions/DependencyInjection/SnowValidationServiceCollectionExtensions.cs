using Microsoft.Extensions.DependencyInjection;
using Snow.Validation;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 校验的服务登记入口
/// </summary>
public static class SnowValidationServiceCollectionExtensions
{
    /// <summary>
    /// 调整校验选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowValidation(this IServiceCollection services, Action<SnowValidationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services;
    }
}
