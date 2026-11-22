using Microsoft.Extensions.DependencyInjection;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// MVC 与自动 API 的服务登记入口
/// </summary>
public static class SnowMvcServiceCollectionExtensions
{
    /// <summary>
    /// 在模块的 PreConfigureServices 阶段调整自动 API 选项
    /// <para>只在 <c>AddApplication</c> 之前调用有效: 本模块的 ConfigureServices 会先应用这些动作再建约定。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">选项配置动作</param>
    /// <returns>服务集合本身, 便于链式调用</returns>
    public static IServiceCollection AddSnowMvc(this IServiceCollection services, Action<SnowMvcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        SnowMvcOptionsSetup? setup = services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowMvcOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowMvcOptionsSetup>()
            .FirstOrDefault();

        if (setup is null)
        {
            setup = new SnowMvcOptionsSetup();
            services.AddSingleton(setup);
        }

        setup.Add(configure);
        return services;
    }
}
