using Snow.Guids;
using Snow.Modularity;
using Snow.MultiTenancy;
using Snow.Threading;
using Snow.Timing;

namespace Snow.Domain;

/// <summary>
/// 领域层模块(对应 ABP 的 <c>AbpDddDomainModule</c>): 只作为加载锚点, 让本程序集进入约定扫描范围
/// <para>实体、值对象、仓储契约都是纯类型; 领域服务基类靠 <c>[TransientService]</c> 注解在子类上参与约定注册,
/// 它用到的时钟 / 租户 / 异步查询执行器由这里声明的依赖模块提供, 标识生成器由 <c>Snow.Guids</c> 提供。</para>
/// </summary>
[DependsOn(typeof(SnowGuidsModule))]
[DependsOn(typeof(SnowThreadingModule))]
[DependsOn(typeof(SnowTimingModule))]
[DependsOn(typeof(SnowMultiTenancyModule))]
public sealed class SnowDomainModule : SnowModule
{
}
