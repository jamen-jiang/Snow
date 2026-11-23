using System.Reflection;
using Mapster;

namespace Snow.ObjectMapping.Mapster;

/// <summary>
/// Mapster 映射选项
/// </summary>
public sealed class SnowObjectMappingMapsterOptions
{
    /// <summary>
    /// 是否启用映射, 关闭后模块不顶替静默实现
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否在启动时编译映射表达式(热路径更快, 首次启动稍慢)
    /// </summary>
    public bool CompileMappings { get; set; }

    /// <summary>
    /// 是否在启动时校验映射配置: 编译一遍, 出错直接抛 <see cref="SnowObjectMappingException"/> 让应用起不来
    /// <para>对应 ABP 的 <c>AbpAutoMapperOptions.ValidatingProfiles</c> + <c>AssertConfigurationIsValid</c>。</para>
    /// </summary>
    public bool ValidateMappings { get; set; }

    /// <summary>
    /// 是否扫描模块程序集里的 <see cref="IRegister"/> 实现并自动应用
    /// </summary>
    public bool ScanRegisters { get; set; } = true;

    /// <summary>
    /// 是否扫描模块程序集里的 <see cref="SnowAutoMapAttribute"/> 并自动建映射
    /// <para>对应 ABP 的 <c>AutoMapAttribute</c>; 关掉就只能靠 <see cref="IRegister"/> / <see cref="SnowMapsterProfile"/> 手工写。</para>
    /// </summary>
    public bool ScanAutoMaps { get; set; } = true;

    /// <summary>
    /// 全局递归深度上限, 没单独配置的映射都用它; 设为 null 表示不限制
    /// <para>对应 ABP 的 <c>AbpAutoMapperOptions.DefaultMaxDepth</c>(默认 64)。</para>
    /// </summary>
    public int? DefaultMaxDepth { get; set; } = 64;

    /// <summary>
    /// 额外登记的配置类型: 不想让扫描扫到的 <see cref="IRegister"/> 实现可在这里点名
    /// </summary>
    public IList<Type> RegisterTypes { get; } = new List<Type>();

    /// <summary>
    /// 额外登记的 <see cref="SnowMapsterProfile"/> 子类, 对应 ABP 的 <c>AbpAutoMapperOptions.AddProfile</c>
    /// </summary>
    public IList<Type> ProfileTypes { get; } = new List<Type>();

    /// <summary>
    /// 额外扫描的程序集, 对应 ABP 的 <c>AbpAutoMapperOptions.AddMaps</c>
    /// <para>只影响 <see cref="IRegister"/> 与 <see cref="SnowAutoMapAttribute"/> 的扫描范围, 不能从配置文件绑定。</para>
    /// </summary>
    public IList<Assembly> MapAssemblies { get; } = new List<Assembly>();

    /// <summary>
    /// 依次执行的配置回调, 对应 ABP 的 <c>AbpAutoMapperOptions.Configurators</c>
    /// </summary>
    public IList<Action<TypeAdapterConfig>> Configurators { get; } = new List<Action<TypeAdapterConfig>>();

    /// <summary>
    /// 直接改配置对象的回调, 在全部 <see cref="IRegister"/> 与 <see cref="Configurators"/> 之后执行
    /// </summary>
    public Action<TypeAdapterConfig>? Configure { get; set; }

    /// <summary>
    /// 登记一个 <see cref="SnowMapsterProfile"/> 子类(对应 ABP 的 <c>AddProfile&lt;TProfile&gt;()</c>)
    /// </summary>
    /// <typeparam name="TProfile">配置类型</typeparam>
    /// <returns>当前选项, 便于链式调用</returns>
    public SnowObjectMappingMapsterOptions AddProfile<TProfile>()
        where TProfile : SnowMapsterProfile
    {
        ProfileTypes.Add(typeof(TProfile));
        return this;
    }

    /// <summary>
    /// 扫描某个模块所在程序集(对应 ABP 的 <c>AddMaps&lt;TModule&gt;()</c>)
    /// </summary>
    /// <typeparam name="TModule">任意位于目标程序集的类型</typeparam>
    /// <returns>当前选项, 便于链式调用</returns>
    public SnowObjectMappingMapsterOptions AddMaps<TModule>()
    {
        MapAssemblies.Add(typeof(TModule).Assembly);
        return this;
    }

    /// <summary>
    /// 扫描给定程序集(对应 ABP 的 <c>AddMaps(params Assembly[])</c>)
    /// </summary>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>当前选项, 便于链式调用</returns>
    public SnowObjectMappingMapsterOptions AddMaps(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (Assembly assembly in assemblies)
        {
            MapAssemblies.Add(assembly);
        }

        return this;
    }
}
