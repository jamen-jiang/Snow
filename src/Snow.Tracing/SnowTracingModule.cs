using Snow.Modularity;

namespace Snow.Tracing;

/// <summary>
/// 链路标识模块(对应 ABP 的 <c>AbpTracingModule</c>): <see cref="CorrelationIdProvider"/> 上的生命周期注解
/// 让它参与约定注册, 本模块只作为加载锚点(依赖它的模块据此确定加载顺序, 并让本程序集进入约定扫描范围)
/// <para>选项不读配置节(与 ABP 一致): 宿主在 <c>PreConfigureServices</c> 里
/// <c>Configure&lt;SnowCorrelationIdOptions&gt;(context.Configuration.GetSection("CorrelationId"))</c> 或直接改属性。</para>
/// </summary>
public sealed class SnowTracingModule : SnowModule;
