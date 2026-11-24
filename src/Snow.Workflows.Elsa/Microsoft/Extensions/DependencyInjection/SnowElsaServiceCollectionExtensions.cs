using Microsoft.Extensions.DependencyInjection;
using Snow.Workflows.Elsa;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 让宿主在自己模块里追加 Elsa 设置的入口。
/// </summary>
public static class SnowElsaServiceCollectionExtensions
{
    /// <summary>
    /// 追加 Elsa 设置。走标准 Options 管道(PostConfigure), 启动时结算, 后登记者覆盖先登记者;
    /// 一般在本模块之前调用。
    /// </summary>
    public static IServiceCollection AddSnowElsa(this IServiceCollection services, Action<SnowElsaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.PostConfigure(configure);
        }

        return services;
    }
}
