using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.Messaging;

/// <summary>
/// 消息模块的显式配置(对应 MediatR 的 <c>MediatRServiceConfiguration</c>)。
/// <para>宿主在模块的 <c>PreConfigureServices</c> 里 <c>Configure&lt;SnowMessagingOptions&gt;(...)</c> 定制;
/// 不再由模块写死扫描范围与行为。</para>
/// </summary>
public sealed class SnowMessagingOptions
{
    /// <summary>是否为经命令总线派发的命令自动开启工作单元(查询始终不开), 默认开启。</summary>
    public bool AutoUnitOfWorkForCommands { get; set; } = true;

    /// <summary>要扫描的程序集; 为空则不注册任何处理器(零配置不再回退, 处理器必须显式登记程序集)。</summary>
    public HashSet<Assembly> Assemblies { get; } = new();

    /// <summary>额外的管道行为; 按加入顺序执行, 先加入的位于外层(先进入、后退出)。</summary>
    public List<SnowMessagingBehavior> Behaviors { get; } = new();

    /// <summary>追加一个待扫描程序集。</summary>
    /// <param name="assembly">程序集</param>
    /// <returns>本配置, 便于链式调用</returns>
    public SnowMessagingOptions RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Assemblies.Add(assembly);
        return this;
    }

    /// <summary>追加多个待扫描程序集。</summary>
    /// <param name="assemblies">程序集</param>
    /// <returns>本配置, 便于链式调用</returns>
    public SnowMessagingOptions RegisterServicesFromAssemblies(params Assembly[] assemblies)
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
    public SnowMessagingOptions RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>追加一个开放泛型管道行为。</summary>
    /// <param name="openBehaviorType">行为类型(开放泛型)</param>
    /// <param name="lifetime">生命周期; 省略为瞬态</param>
    /// <returns>本配置, 便于链式调用</returns>
    public SnowMessagingOptions AddOpenBehavior(Type openBehaviorType, ServiceLifetime? lifetime = null)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);
        Behaviors.Add(new SnowMessagingBehavior(openBehaviorType, lifetime ?? ServiceLifetime.Transient));
        return this;
    }

    /// <summary>追加一个开放泛型管道行为。</summary>
    /// <typeparam name="TBehavior">行为类型</typeparam>
    /// <param name="lifetime">生命周期; 省略为瞬态</param>
    /// <returns>本配置, 便于链式调用</returns>
    public SnowMessagingOptions AddOpenBehavior<TBehavior>(ServiceLifetime? lifetime = null)
        where TBehavior : class
        => AddOpenBehavior(typeof(TBehavior), lifetime);
}

/// <summary>一条开放泛型管道行为的登记信息。</summary>
/// <param name="Type">行为类型(开放泛型, 如 <c>LoggingBehavior&lt;,&gt;</c>)</param>
/// <param name="Lifetime">生命周期</param>
public sealed record SnowMessagingBehavior(Type Type, ServiceLifetime Lifetime);
