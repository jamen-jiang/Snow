using Microsoft.Extensions.DependencyInjection;
using Snow.Validation.FluentValidation;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// FluentValidation 集成的服务登记入口
/// </summary>
public static class SnowFluentValidationServiceCollectionExtensions
{
    /// <summary>
    /// 调整 FluentValidation 集成选项。走标准 Options 管道(PostConfigure), 启动时结算
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowFluentValidation(this IServiceCollection services, Action<SnowFluentValidationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.PostConfigure(configure);
        return services;
    }
}
