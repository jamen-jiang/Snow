using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.EventBus;

/// <summary>
/// 事件总线的显式配置(对标 MediatR 的配置对象)。
/// <para>宿主在模块的 <c>PreConfigureServices</c> 里 <c>Configure&lt;SnowEventBusOptions&gt;(...)</c> 定制;
/// 不再由模块全量扫描所有模块程序集。</para>
/// </summary>
public sealed class SnowEventBusOptions
{
    /// <summary>要扫描的程序集; 为空则不注册任何处理器(处理器必须显式登记程序集)。</summary>
    public HashSet<Assembly> Assemblies { get; } = new();

    /// <summary>处理器生命周期, 默认瞬态。</summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>追加一个待扫描程序集。</summary>
    /// <param name="assembly">程序集</param>
    /// <returns>本配置, 便于链式调用</returns>
    public SnowEventBusOptions RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Assemblies.Add(assembly);
        return this;
    }

    /// <summary>追加多个待扫描程序集。</summary>
    /// <param name="assemblies">程序集</param>
    /// <returns>本配置, 便于链式调用</returns>
    public SnowEventBusOptions RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        foreach (Assembly assembly in assemblies)
        {
            Assemblies.Add(assembly);
        }

        return this;
    }

    /// <summary>追加 <typeparamref name="T"/> 所在程序集。</summary>
    /// <typeparam name="T">用于定位程序集的类型</typeparam>
    /// <returns>本配置, 便于链式调用</returns>
    public SnowEventBusOptions RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);
}
