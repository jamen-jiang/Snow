using System.Reflection;

namespace Rongia.Modularity;

/// <summary>
/// 模块描述信息(对应 ABP 的 AbpModuleDescriptor)
/// </summary>
public class ModuleDescriptor
{
    /// <summary>
    /// 模块类型
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// 模块类型所在的程序集,也是约定扫描的范围
    /// </summary>
    public Assembly Assembly { get; }

    /// <summary>
    /// 模块实例
    /// </summary>
    public Module Instance { get; }

    /// <summary>
    /// 直接依赖的模块,被依赖者在前
    /// </summary>
    public IReadOnlyList<ModuleDescriptor> Dependencies => _dependencies;

    private readonly List<ModuleDescriptor> _dependencies;
    private readonly HashSet<ModuleDescriptor> _dependencySet;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="type">模块类型</param>
    /// <param name="instance">模块实例</param>
    public ModuleDescriptor(Type type, Module instance)
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
        Instance = instance;

        _dependencies = new List<ModuleDescriptor>();
        _dependencySet = new HashSet<ModuleDescriptor>();
    }

    /// <summary>
    /// 追加一个直接依赖的模块,已存在时不重复添加
    /// </summary>
    /// <param name="descriptor">被依赖模块的描述信息</param>
    public void AddDependency(ModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (_dependencySet.Add(descriptor))
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
