using Snow.Modularity;

namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁模块(与 ABP 的 <c>AbpDistributedLockingModule</c> 一致): 承载契约、选项、key 归一化、进程内默认实现, 并提供基于 Medallion 的分布式实现。
/// <para>本模块只依赖 <c>DistributedLock.Core</c>; 具体后端(Redis / Postgres / SqlServer …)由宿主自己引用对应的
/// <c>DistributedLock.&lt;Provider&gt;</c> 包并注册 <c>IDistributedLockProvider</c>, 与 ABP 的做法一致。</para>
/// <para>没有注册提供者时, <see cref="MedallionSnowDistributedLock"/> 会因解析不到 <c>IDistributedLockProvider</c> 而失败, 这点也与 ABP 相同。</para>
/// <para><see cref="SnowDistributedLockOptions"/> 只放 key 前缀, 由使用方自行 <c>context.Services.Configure&lt;SnowDistributedLockOptions&gt;(...)</c> 设置, 不绑配置节(与 ABP 一致)。</para>
/// </summary>
public sealed class SnowDistributedLockingModule : SnowModule
{
}
