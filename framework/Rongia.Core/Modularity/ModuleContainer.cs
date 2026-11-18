namespace Rongia.Modularity;

/// <summary>
/// 模块容器的默认实现:构造时按类型建立索引
/// </summary>
public sealed class ModuleContainer : IModuleContainer
{
    private readonly Dictionary<Type, ModuleDescriptor> _byType = new Dictionary<Type, ModuleDescriptor>();

    /// <inheritdoc />
    public IReadOnlyList<ModuleDescriptor> Modules { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="modules">已按加载顺序排好的模块列表</param>
    public ModuleContainer(IReadOnlyList<ModuleDescriptor> modules)
    {
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));

        foreach (ModuleDescriptor module in modules)
        {
            _byType[module.Type] = module;
        }
    }

    /// <inheritdoc />
    public ModuleDescriptor? Find(Type moduleType)
    {
        if (moduleType == null)
        {
            return null;
        }

        return _byType.TryGetValue(moduleType, out ModuleDescriptor? descriptor) ? descriptor : null;
    }
}
