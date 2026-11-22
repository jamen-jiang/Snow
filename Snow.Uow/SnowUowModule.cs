using Snow.Modularity;

namespace Snow.Uow;

/// <summary>
/// 工作单元模块: 服务由 <see cref="UnitOfWorkAccessor"/> 与 <see cref="SnowUnitOfWorkManager"/>
/// 上的生命周期注解登记, 本模块只作为加载锚点(依赖它的模块据此确定加载顺序)
/// </summary>
public sealed class SnowUowModule : SnowModule
{
}
