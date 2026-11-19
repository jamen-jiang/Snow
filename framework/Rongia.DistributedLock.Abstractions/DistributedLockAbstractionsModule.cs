using Rongia.Modularity;

namespace Rongia.DistributedLock;

/// <summary>
/// 分布式锁契约模块(对应 ABP 的 AbpDistributedLockingAbstractionsModule)
/// <para>不显式登记任何实现:默认的 <see cref="LocalDistributedLock"/>(单例)与 <see cref="DistributedLockKeyNormalizer"/>(瞬态)
/// 靠生命周期标记由约定注册自动登记。</para>
/// <para>空模块的职责只在于成为模块图锚点: 没有它, 本程序集不会被约定扫描; 而且它给实现包一个可 <c>[DependsOn]</c> 的目标,
/// 保证"先扫契约、再扫实现"的替换顺序。Options 基础设施由 <c>Application</c> 全局注册, 无需在此重复。</para>
/// </summary>
public sealed class DistributedLockAbstractionsModule : Module { }
