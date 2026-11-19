namespace Rongia.Modularity;

/// <summary>
/// 模块加载结果:按依赖顺序排列的模块列表,加上按类型索引的模块字典。
/// <para>字典在加载阶段就已建好,直接交给 <see cref="ModuleContainer"/> 复用,容器不再重复遍历列表建索引。</para>
/// </summary>
public sealed class ModuleLoadResult
{
    /// <summary>
    /// 按依赖顺序排列的模块描述信息,被依赖者在前,启动模块排在最后
    /// </summary>
    public IReadOnlyList<RongiaModuleDescriptor> Modules { get; }

    /// <summary>
    /// 模块类型到模块描述信息的索引,与 <see cref="Modules"/> 内容一致
    /// </summary>
    public IReadOnlyDictionary<Type, RongiaModuleDescriptor> ModulesByType { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="modules">按依赖顺序排列的模块描述信息</param>
    /// <param name="modulesByType">模块类型到模块描述信息的索引</param>
    public ModuleLoadResult(
        IReadOnlyList<RongiaModuleDescriptor> modules,
        IReadOnlyDictionary<Type, RongiaModuleDescriptor> modulesByType)
    {
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        ModulesByType = modulesByType ?? throw new ArgumentNullException(nameof(modulesByType));
    }
}
