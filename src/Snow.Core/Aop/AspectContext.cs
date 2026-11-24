using System.Reflection;

namespace Snow.Aop;

/// <summary>
/// 一次被拦截调用的全部信息, 整条拦截链共享这一个对象: 实参的原地修改与 <see cref="ReturnValue"/> 对上下层都可见。
/// <para>模型与 ASP.NET Core 的中间件一致: 上下文只装数据, 下一环由 <see cref="AspectDelegate"/> 参数传入,
/// 所以这里没有 ProceedAsync, 也没有可变指针。</para>
/// <para>抽象类只定义形状, 不带状态也不规定构造: 拦截器只认这个类型, 具体怎么造由代理工厂决定 ——
/// Snow.Aop.Castle 提供包住 Castle 调用信息的 <c>DefaultAspectContext</c>。</para>
/// </summary>
public abstract class AspectContext
{
    /// <summary>
    /// 实参数组, 可原地修改
    /// </summary>
    public abstract object?[] Arguments { get; }

    /// <summary>
    /// 按参数名索引实参的只读视图
    /// </summary>
    public abstract IReadOnlyDictionary<string, object?> ArgumentsDictionary { get; }

    /// <summary>
    /// 泛型方法的类型实参; 非泛型方法为 <c>null</c>
    /// </summary>
    public abstract Type[]? GenericArguments { get; }

    /// <summary>
    /// 被代理的目标对象
    /// </summary>
    public abstract object? TargetObject { get; }

    /// <summary>
    /// 被调用的方法
    /// </summary>
    public abstract MethodInfo Method { get; }

    /// <summary>
    /// 目标方法的返回值: 拦截器可读取以包装结果, 也可直接赋值以实现短路或缓存
    /// </summary>
    public abstract object? ReturnValue { get; set; }

    /// <summary>
    /// 服务提供者: 特性无法构造注入, 需要其他服务时从这里解析
    /// </summary>
    public abstract IServiceProvider ServiceProvider { get; }
}
