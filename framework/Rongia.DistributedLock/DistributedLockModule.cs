using Rongia.Modularity;

namespace Rongia.DistributedLock;

/// <summary>
/// Medallion 分布式锁模块(对应 ABP 的 AbpDistributedLockingModule):依赖契约模块,自身不登记任何服务 ——
/// 替换由 <see cref="MedallionDistributedLock"/> 上的 <c>[Dependency(ReplaceServices = true)]</c> 经约定注册完成。
/// <para>宿主必须先注册一个 Medallion 的 <see cref="Medallion.Threading.IDistributedLockProvider"/>,例如:
/// <c>services.AddSingleton&lt;IDistributedLockProvider&gt;(new RedisDistributedSynchronizationProvider(multiplexer));</c></para>
/// </summary>
[DependsOn(typeof(DistributedLockAbstractionsModule))]
public sealed class DistributedLockModule : Module
{
}
