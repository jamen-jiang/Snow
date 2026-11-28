using Microsoft.Extensions.DependencyInjection;
using Snow.Castle.DynamicProxy;
using Snow.Modularity;

namespace Snow.Castle;

/// <summary>
/// Castle 模块: 提供代理工厂, 并登记开放泛型的拦截器适配器。
/// <para>Core 只声明 <c>ISnowProxyFactory</c>, 本包用 Castle 的 <c>ProxyGenerator</c> 完成实现。</para>
/// </summary>
public class SnowCastleCoreModule : SnowModule
{
    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient(typeof(SnowAsyncDeterminationInterceptor<>));
    }
}
