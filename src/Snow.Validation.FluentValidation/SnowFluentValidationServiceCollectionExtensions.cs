using Microsoft.Extensions.DependencyInjection;

namespace Snow.Validation.FluentValidation;

/// <summary>
/// FluentValidation 集成的服务登记入口
/// </summary>
public static class SnowFluentValidationServiceCollectionExtensions
{
    /// <summary>
    /// 调整 FluentValidation 集成选项。必须在 <c>AddApplication</c> 之前调用
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowFluentValidation(this IServiceCollection services, Action<SnowFluentValidationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        SnowFluentValidationOptionsSetup? setup = services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowFluentValidationOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowFluentValidationOptionsSetup>()
            .FirstOrDefault();

        if (setup is null)
        {
            setup = new SnowFluentValidationOptionsSetup();
            services.AddSingleton(setup);
        }

        setup.Add(configure);
        return services;
    }
}
