using Snow.Modularity;

namespace Snow.Timing;

/// <summary>
/// 时间模块(对应 ABP 的 <c>AbpTimingModule</c>): <see cref="Clock"/> 上的生命周期注解
/// 让它参与约定注册, 本模块只作为加载锚点(依赖它的模块据此确定加载顺序, 并让本程序集进入约定扫描范围)
/// </summary>
public sealed class SnowTimingModule : SnowModule
{
}
