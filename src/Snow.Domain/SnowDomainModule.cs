using Snow.Guids;
using Snow.Modularity;
using Snow.MultiTenancy;
using Snow.Timing;

namespace Snow.Domain;

/// <summary>
/// 领域层模块(对应 ABP 的 <c>AbpDddDomainModule</c>): 只作为加载锚点, 让本程序集进入约定扫描范围
/// <para>实体、值对象、仓储契约都是纯类型; 领域服务子类通过构造器声明依赖。</para>
/// </summary>
[DependsOn(typeof(SnowGuidsModule))]
[DependsOn(typeof(SnowTimingModule))]
[DependsOn(typeof(SnowMultiTenancyModule))]
public sealed class SnowDomainModule : SnowModule
{
}
