using Mapster;

namespace Snow.ObjectMapping.Mapster;

/// <summary>
/// 映射配置基类: 对应 ABP 的 <c>MapperProfile</c>(AutoMapper 的 Profile), 只是配置方式是 Mapster
/// <para>继承它并调用 <c>CreateMap&lt;TSource, TDestination&gt;()</c>, 模块启动时自动应用; 子类不需要打生命周期注解。</para>
/// <para>写法上等价于直接实现 Mapster 的 <see cref="IRegister"/>, 区别只是多了 CreateMap 这几个顺手方法。</para>
/// </summary>
public abstract class SnowMapsterProfile : IRegister
{
    private readonly List<Action<TypeAdapterConfig>> _maps = [];

    /// <summary>
    /// 声明一个映射: 按名称自动对齐成员
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    protected void CreateMap<TSource, TDestination>()
    {
        _maps.Add(config => config.NewConfig<TSource, TDestination>());
    }

    /// <summary>
    /// 声明一个映射, 并自定义成员规则
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="configure">配置回调, 拿到的是这一次映射的规则对象</param>
    protected void CreateMap<TSource, TDestination>(Action<TypeAdapterSetter<TSource, TDestination>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _maps.Add(config => configure(config.NewConfig<TSource, TDestination>()));
    }

    /// <summary>
    /// 声明一个映射: 类型在运行时才知道(泛型封装的场合用)
    /// </summary>
    /// <param name="sourceType">源类型</param>
    /// <param name="destinationType">目标类型</param>
    protected void CreateMap(Type sourceType, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(destinationType);

        _maps.Add(config => config.NewConfig(sourceType, destinationType));
    }

    /// <summary>
    /// 声明双向映射
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    protected void CreateTwoWays<TSource, TDestination>()
    {
        _maps.Add(config => config.NewConfig<TSource, TDestination>());
        _maps.Add(config => config.NewConfig<TDestination, TSource>());
    }

    /// <summary>
    /// 应用全部声明的映射, 再执行子类的自定义配置
    /// </summary>
    /// <param name="config">映射配置对象</param>
    void IRegister.Register(TypeAdapterConfig config)
    {
        foreach (Action<TypeAdapterConfig> map in _maps)
        {
            map(config);
        }

        Configure(config);
    }

    /// <summary>
    /// 子类的自定义配置, 在 CreateMap 声明的映射之后执行
    /// </summary>
    /// <param name="config">映射配置对象</param>
    protected virtual void Configure(TypeAdapterConfig config)
    {
    }
}
