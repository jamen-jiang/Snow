namespace Rongia.Modularity;

/// <summary>
/// 模块容器的默认实现(对应 ABP 的 ModuleContainer)
/// </summary>
public class ModuleContainer : IModuleContainer
{
    /// <inheritdoc />
    public IReadOnlyList<RongiaModuleDescriptor> Modules { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<Type, RongiaModuleDescriptor> ModulesByType { get; }

    /// <summary>
    /// 直接复用加载器建好的索引,不重复遍历模块列表
    /// </summary>
    /// <param name="loadResult">模块加载结果</param>
    public ModuleContainer(ModuleLoadResult loadResult)
    {
        ArgumentNullException.ThrowIfNull(loadResult);

        Modules = loadResult.Modules;
        ModulesByType = loadResult.ModulesByType;
    }

    /// <summary>
    /// 构造器:只给模块列表时自行建立类型索引
    /// </summary>
    /// <param name="modules">已按加载顺序排好的模块列表</param>
    /// <exception cref="ModuleLoadException">同一模块类型出现两次</exception>
    public ModuleContainer(IReadOnlyList<RongiaModuleDescriptor> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        Modules = modules;

        Dictionary<Type, RongiaModuleDescriptor> modulesByType = new Dictionary<Type, RongiaModuleDescriptor>(modules.Count);

        foreach (RongiaModuleDescriptor module in modules)
        {
            if (!modulesByType.TryAdd(module.Type, module))
            {
                throw new ModuleLoadException("同一个模块类型被加载了两次: " + module.Type.AssemblyQualifiedName);
            }
        }

        ModulesByType = modulesByType;
    }

    /// <inheritdoc />
    public RongiaModuleDescriptor? Find(Type moduleType)
    {
        ArgumentNullException.ThrowIfNull(moduleType);

        return ModulesByType.TryGetValue(moduleType, out RongiaModuleDescriptor? module) ? module : null;
    }
}
