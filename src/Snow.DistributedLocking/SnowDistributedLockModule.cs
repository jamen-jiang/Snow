using Snow.Modularity;

namespace Snow.DistributedLocking;

/// <summary>
/// 分布式锁模块: 契约(<see cref="ISnowDistributedLock"/> / 句柄 / 选项 / <see cref="DistributedLockAttribute"/> 注解)、
/// 默认的进程内实现(<see cref="LocalSnowDistributedLock"/>)与 Redisson 式看门狗(<see cref="DistributedLockWatchdog"/>)。
/// <para><see cref="SnowDistributedLockOptions"/> 只有 key 前缀一项, 由使用方自行
/// <c>context.Services.Configure&lt;SnowDistributedLockOptions&gt;(...)</c> 设置, 不绑配置节(与 ABP 一致);
/// 前缀怎么叠进 key 由 <see cref="IDistributedLockKeyNormalizer"/> 统一处理。</para>
/// <para>本模块不含远程后端 —— 单机直接用默认的 <see cref="LocalSnowDistributedLock"/>, 多实例部署再加
/// <c>Snow.DistributedLocking.Redis</c>, 由它的 <c>RedisSnowDistributedLock</c> 按
/// <c>[SingletonService(ReplaceServices = true)]</c> 顶替默认锁。</para>
/// </summary>
public sealed class SnowDistributedLockModule : SnowModule;
