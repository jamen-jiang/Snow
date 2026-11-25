using Snow.Modularity;
using Snow.Timing;

namespace Snow.Json;

/// <summary>
/// JSON 模块(对应 ABP 的 <c>AbpJsonModule</c> 与 <c>AbpJsonSystemTextJsonModule</c> 合体)
/// <para>本包自带 System.Text.Json 实现, 默认就生效; 要换 Newtonsoft.Json 再装 Snow.Json.Newtonsoft。</para>
/// <para>选项不读配置节(与 ABP 一致): 宿主在 <c>PreConfigureServices</c> 里
/// <c>Configure&lt;SnowJsonOptions&gt;(options => ...)</c> 或 <c>Configure&lt;SnowJsonOptions&gt;(context.Configuration.GetSection("Json"))</c> 给出。</para>
/// </summary>
[DependsOn(typeof(SnowTimingModule))]
public sealed class SnowJsonModule : SnowModule;
