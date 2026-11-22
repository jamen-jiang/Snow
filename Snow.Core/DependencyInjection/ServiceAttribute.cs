using Microsoft.Extensions.DependencyInjection;

namespace Snow.DependencyInjection;

/// <summary>
/// 约定注册的生命周期注解: 标在类上即按该生命周期登记, 不需要实现任何标记接口。
/// <para>生命周期必须写明, 没有默认值 —— 直接写 <c>[Service(ServiceLifetime.Singleton)]</c>;
/// 三种常用的有简称: <see cref="SingletonServiceAttribute"/> / <see cref="ScopedServiceAttribute"/> /
/// <see cref="TransientServiceAttribute"/>。</para>
/// <para>标注可继承(<c>Inherited = true</c>): 标在抽象基类上, 子类即使不写也按同一生命周期登记 ——
/// 权限定义提供器与权限值提供器就靠这一点, 宿主只需继承基类。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class ServiceAttribute : Attribute
{
    /// <summary>
    /// 构造器: 必须给出生命周期。故意不提供无参构造器, 免得 <c>[Service]</c> 悄悄落到某个默认值上
    /// </summary>
    /// <param name="lifetime">生命周期</param>
    public ServiceAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// 生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// 是否替换已有同服务类型的注册
    /// </summary>
    public bool ReplaceServices { get; set; }
}

/// <summary>
/// 单例: 整个应用一个实例。只放无状态或明确要全局共享的东西(缓存、计数器、连接池持有者)
/// </summary>
public sealed class SingletonServiceAttribute : ServiceAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    public SingletonServiceAttribute()
        : base(ServiceLifetime.Singleton)
    {
    }
}

/// <summary>
/// 作用域: 一个请求(一个 <see cref="IServiceScope"/>)一个实例, 与 DbContext 的作用域一致
/// </summary>
public sealed class ScopedServiceAttribute : ServiceAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    public ScopedServiceAttribute()
        : base(ServiceLifetime.Scoped)
    {
    }
}

/// <summary>
/// 瞬态: 每次解析都新建。只放轻量无状态的对象, 别在它身上挂一次性资源
/// </summary>
public sealed class TransientServiceAttribute : ServiceAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    public TransientServiceAttribute()
        : base(ServiceLifetime.Transient)
    {
    }
}
