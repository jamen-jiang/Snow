using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;

namespace Snow.Tracing;

/// <summary>
/// 链路标识模块(对应 ABP 的 <c>AbpTracingModule</c>): <see cref="CorrelationIdProvider"/> 上的生命周期注解
/// 让它参与约定注册, 本模块只作为加载锚点(依赖它的模块据此确定加载顺序, 并让本程序集进入约定扫描范围)
/// <para>另有配置节 "CorrelationId" 绑定到 <see cref="SnowCorrelationIdOptions"/>。</para>
/// </summary>
public sealed class SnowTracingModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Configuration.GetSection("CorrelationId").Exists())
        {
            Configure<SnowCorrelationIdOptions>(context.Configuration.GetSection("CorrelationId"));
        }
    }
}
