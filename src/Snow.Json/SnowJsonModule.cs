using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;

namespace Snow.Json;

/// <summary>
/// JSON 模块(对应 ABP 的 <c>AbpJsonModule</c> 与 <c>AbpJsonSystemTextJsonModule</c> 合体)
/// <para>本包自带 System.Text.Json 实现, 默认就生效; 要换 Newtonsoft.Json 再装 Snow.Json.Newtonsoft。</para>
/// <para>公共开关读 <c>Json</c> 节。</para>
/// </summary>
public sealed class SnowJsonModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Configuration.GetSection("Json").Exists())
        {
            Configure<SnowJsonOptions>(context.Configuration.GetSection("Json"));
        }
    }
}
