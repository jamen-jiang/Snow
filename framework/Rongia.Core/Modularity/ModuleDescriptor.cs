using System.Reflection;

namespace Rongia.Modularity;

/// <summary>
/// 模块描述信息的默认实现(对应 ABP 的 AbpModuleDescriptor)
/// </summary>
public class ModuleDescriptor : IModuleDescriptor
{
    /// <inheritdoc />
    public Type Type { get; }

    /// <inheritdoc />
    public Assembly Assembly { get; }

    /// <inheritdoc />
    public Assembly[] AllAssemblies { get; }

    /// <inheritdoc />
    public IModule Instance { get; }

    /// <inheritdoc />
    public bool IsLoadedAsPlugIn { get; }

    /// <inheritdoc />
    public IReadOnlyList<IModuleDescriptor> Dependencies => _dependencies;

    private readonly List<IModuleDescriptor> _dependencies;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="type">模块类型</param>
    /// <param name="instance">模块实例</param>
    /// <param name="isLoadedAsPlugIn">是否以插件方式加载</param>
    public ModuleDescriptor(Type type, IModule instance, bool isLoadedAsPlugIn = false)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(instance);

        ModuleHelper.CheckModuleType(type);

        if (!type.IsInstanceOfType(instance))
        {
            throw new ArgumentException(
                "给定的模块实例(" + instance.GetType().AssemblyQualifiedName + ")不是给定模块类型的实例: " + type.AssemblyQualifiedName);
        }

        Type = type;
        Assembly = type.Assembly;
        AllAssemblies = ModuleHelper.GetAllAssemblies(type);
        Instance = instance;
        IsLoadedAsPlugIn = isLoadedAsPlugIn;

        _dependencies = new List<IModuleDescriptor>();
    }

    /// <summary>
    /// 追加一个直接依赖的模块,已存在时不重复添加
    /// </summary>
    /// <param name="descriptor">被依赖模块的描述信息</param>
    public void AddDependency(IModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_dependencies.Contains(descriptor))
        {
            _dependencies.Add(descriptor);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "[ModuleDescriptor " + Type.FullName + "]";
    }
}
