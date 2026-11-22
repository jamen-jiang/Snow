using Snow.Modularity;

namespace Snow.Aop.Castle;

/// <summary>
/// AOP 模块: 只作为加载锚点。代理工厂 <see cref="CastleAopProxyFactory"/> 上的生命周期注解让它参与约定注册,
/// 实现类型上的 <c>[Interceptor]</c> 由约定注册扫描时读取并生成代理。
/// <para>不额外提供 IServiceCollection 扩展方法, 组件级注册一律写在模块里, 与 ABP 的 AbpCastleCoreModule 一致。</para>
/// </summary>
public sealed class SnowAopModule : SnowModule
{
}
