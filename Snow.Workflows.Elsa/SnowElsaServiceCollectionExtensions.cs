using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Snow.Workflows.Elsa;

/// <summary>
/// 让宿主在自己模块里追加 Elsa 设置的入口。
/// </summary>
public static class SnowElsaServiceCollectionExtensions
{
    /// <summary>
    /// 追加 Elsa 设置。请在自己的模块 PreConfigureServices 里调用(那时所有模块还没开始注册服务),
    /// 或在 AddApplication 之前调用; 若在 ConfigureServices 里调用, 因为 SnowElsaModule 可能已经跑过, 设置不会生效。
    /// </summary>
    public static IServiceCollection AddSnowElsa(this IServiceCollection services, Action<SnowElsaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        SnowElsaOptionsSetup? setup = services
            .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(SnowElsaOptionsSetup))
            ?.ImplementationInstance as SnowElsaOptionsSetup;

        if (setup is null)
        {
            setup = new SnowElsaOptionsSetup();
            services.AddSingleton(setup);
        }

        if (configure is not null)
        {
            setup.Add(configure);
        }

        return services;
    }
}
