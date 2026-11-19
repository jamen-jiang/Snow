using System.Reflection;

namespace Rongia.Modularity;

/// <summary>
/// 模块描述信息(对应 ABP 的 AbpModuleDescriptor)
/// </summary>
public class RongiaModuleDescriptor
{
    /// <summary>
    /// 模块类型
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// 模块类型所在的程序集(模块主程序集)
    /// </summary>
    public Assembly Assembly { get; }

    /// <summary>
    /// 模块参与约定扫描的全部程序集:主程序集加上
    /// <see cref="AdditionalAssemblyAttribute"/> 声明的额外程序集
    /// </summary>
    public Assembly[] AllAssemblies { get; }

    /// <summary>
    /// 模块实例
    /// </summary>
    public RongiaModule Instance { get; }

    /// <summary>
    /// 直接依赖的模块,被依赖者在前
    /// </summary>
    public IReadOnlyList<RongiaModuleDescriptor> Dependencies => _dependencies;

    private readonly List<RongiaModuleDescriptor> _dependencies;
    private readonly HashSet<RongiaModuleDescriptor> _dependencySet;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="type">模块类型</param>
    /// <param name="instance">模块实例</param>
    public RongiaModuleDescriptor(Type type, RongiaModule instance)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(instance);

        RongiaModuleHelper.CheckModuleType(type);

        if (!type.IsInstanceOfType(instance))
        {
            throw new ArgumentException(
                "给定的模块实例(" + instance.GetType().AssemblyQualifiedName + ")不是给定模块类型的实例: " + type.AssemblyQualifiedName);
        }

        Type = type;
        Assembly = type.Assembly;
        AllAssemblies = RongiaModuleHelper.GetAllAssemblies(type);
        Instance = instance;

        _dependencies = new List<RongiaModuleDescriptor>();
        _dependencySet = new HashSet<RongiaModuleDescriptor>();
    }

    /// <summary>
    /// 追加一个直接依赖的模块,已存在时不重复添加
    /// </summary>
    /// <param name="descriptor">被依赖模块的描述信息</param>
    public void AddDependency(RongiaModuleDescriptor descriptor)
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
        return "[RongiaModuleDescriptor " + Type.FullName + "]";
    }
}
