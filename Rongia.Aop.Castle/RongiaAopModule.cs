using Microsoft.Extensions.DependencyInjection;
using Rongia.Aop;
using Rongia.Modularity;

namespace Rongia.Aop.Castle;

/// <summary>
/// AOP 模块: 注册代理工厂。实现类型上的 <c>[Interceptor]</c> 由约定注册扫描时读取并生成代理。
/// <para>不额外提供 IServiceCollection 扩展方法, 组件级注册一律写在模块里, 与 ABP 的 AbpCastleCoreModule 一致。</para>
/// </summary>
public sealed class RongiaAopModule : RongiaModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddSingleton<IAopProxyFactory, CastleAopProxyFactory>();
    }
}
