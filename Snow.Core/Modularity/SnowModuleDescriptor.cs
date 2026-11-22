using System.Reflection;

namespace Snow.Modularity;

/// <summary>
/// 模块描述信息(对应 ABP 的 AbpModuleDescriptor)
/// </summary>
public class SnowModuleDescriptor
{
    /// <summary>
    /// 模块类型
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// 模块参与约定扫描的程序集,即模块自身所在的程序集
    /// </summary>
    public Assembly[] AllAssemblies { get; }

    /// <summary>
    /// 模块实例
    /// </summary>
    public SnowModule Instance { get; }

    /// <summary>
    /// 直接依赖的模块,被依赖者在前
    /// </summary>
    public IReadOnlyList<SnowModuleDescriptor> Dependencies => _dependencies;

    private readonly List<SnowModuleDescriptor> _dependencies;
    private readonly HashSet<SnowModuleDescriptor> _dependencySet;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="type">模块类型</param>
    /// <param name="instance">模块实例</param>
    public SnowModuleDescriptor(Type type, SnowModule instance)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(instance);

        SnowModuleHelper.CheckModuleType(type);

        if (!type.IsInstanceOfType(instance))
        {
            throw new ArgumentException(
                "给定的模块实例(" + instance.GetType().AssemblyQualifiedName + ")不是给定模块类型的实例: " + type.AssemblyQualifiedName);
        }

        Type = type;
        AllAssemblies = [type.Assembly];
        Instance = instance;

        _dependencies = [];
        _dependencySet = [];
    }

    /// <summary>
    /// 追加一个直接依赖的模块,已存在时不重复添加
    /// </summary>
    /// <param name="descriptor">被依赖模块的描述信息</param>
    public void AddDependency(SnowModuleDescriptor descriptor)
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
        return "[SnowModuleDescriptor " + Type.FullName + "]";
    }
}
