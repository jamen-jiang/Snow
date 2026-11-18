namespace Rongia.Modularity;

/// <summary>
/// 模块描述信息:记录模块类型、实例与它直接声明的依赖
/// </summary>
public sealed class ModuleDescriptor
{
    /// <summary>
    /// 模块类型
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// 模块实例,加载阶段创建一次并复用
    /// </summary>
    public Module Instance { get; }

    /// <summary>
    /// 通过 DependsOnAttribute 直接声明的依赖模块类型
    /// </summary>
    public IReadOnlyList<Type> Dependencies { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="type">模块类型</param>
    /// <param name="instance">模块实例</param>
    /// <param name="dependencies">直接依赖的模块类型</param>
    public ModuleDescriptor(Type type, Module instance, IReadOnlyList<Type> dependencies)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        Dependencies = dependencies ?? Type.EmptyTypes;
    }
}