namespace Snow.Modularity;

/// <summary>
/// 模块容器的默认实现(对应 ABP 的 ModuleContainer)
/// </summary>
public sealed class ModuleContainer : IModuleContainer
{
    /// <inheritdoc />
    public IReadOnlyList<SnowModuleDescriptor> Modules { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="modules">已按加载顺序排好的模块列表</param>
    public ModuleContainer(IReadOnlyList<SnowModuleDescriptor> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        Modules = modules;
    }
}
