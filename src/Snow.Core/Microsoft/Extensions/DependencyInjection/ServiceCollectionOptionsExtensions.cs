using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Snow.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 选项登记扩展, 对应 ABP 的 <c>ServiceCollectionOptionsExtensions</c>。
/// </summary>
public static class ServiceCollectionOptionsExtensions
{
    /// <summary>
    /// 登记选项并替换为 <see cref="SnowUnnamedOptionsManager{TOptions}"/>。
    /// <para>只有在选项配置过程中还需要通过 ServiceProvider 读取其他选项时才需要使用本方法; 否则直接用 AddOptions。</para>
    /// </summary>
    /// <typeparam name="TOptions">选项类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>选项构建器</returns>
    public static OptionsBuilder<TOptions> AddSnowOptions<TOptions>(this IServiceCollection services)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IOptions<TOptions>, SnowUnnamedOptionsManager<TOptions>>();
        return services.AddOptions<TOptions>();
    }
}
