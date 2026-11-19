using Microsoft.Extensions.DependencyInjection;

namespace Rongia.DependencyInjection;

/// <summary>
/// 显式声明注册方式,覆盖标记接口推断出的生命周期(对应 ABP DependencyAttribute)
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DependencyAttribute : Attribute
{
    /// <summary>
    /// 指定生命周期;为 null 时回退到标记接口推断
    /// </summary>
    public ServiceLifetime? Lifetime { get; set; }

    /// <summary>
    /// 保留的 ABP 兼容属性:true 表示用 TryAdd 语义注册
    /// </summary>
    /// <remarks>Rongia 的约定注册本身幂等(同一服务类型加同一实现类型不会重复登记),该属性只表达意图,不改变结果。</remarks>
    public bool TryRegister { get; set; }

    /// <summary>
    /// 是否替换已有同服务类型的注册
    /// </summary>
    public bool ReplaceServices { get; set; }

    /// <summary>
    /// 无参构造器:全部走默认推断
    /// </summary>
    public DependencyAttribute()
    {
    }

    /// <summary>
    /// 构造器:直接指定生命周期
    /// </summary>
    /// <param name="lifetime">生命周期</param>
    public DependencyAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}
