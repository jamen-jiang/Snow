using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;

namespace Snow.Json.Newtonsoft;

/// <summary>
/// Newtonsoft.Json 实现模块: 读 <c>Json:Newtonsoft</c> 配置节并顶替静默实现
/// <para>Snow.Json 自带 System.Text.Json 实现, 装本包即以 ReplaceServices 顶替它; 两个实现按模块加载顺序, 后者胜。</para>
/// </summary>
[DependsOn(typeof(SnowJsonModule))]
public sealed class SnowJsonNewtonsoftModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Configuration.GetSection("Json:Newtonsoft").Exists())
        {
            Configure<SnowNewtonsoftJsonOptions>(context.Configuration.GetSection("Json:Newtonsoft"));
        }
    }
}
