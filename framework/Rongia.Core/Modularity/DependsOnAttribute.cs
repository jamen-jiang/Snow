using System.Reflection;
using Module = Rongia.Modularity.Module;

namespace Rongia.Modularity;

/// <summary>
/// 声明模块之间的依赖关系。加载器会先加载被依赖的模块,再加载此模块。
/// 可多次标注,每次传入若干被依赖的模块类型。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class DependsOnAttribute : Attribute, IDependedTypesProvider
{
    /// <summary>
    /// 被依赖的模块类型集合
    /// </summary>
    public Type[] DependedTypes { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="dependedTypes">被依赖的模块类型,必须继承 Module</param>
    public DependsOnAttribute(params Type[]? dependedTypes)
    {
        DependedTypes = dependedTypes ?? Type.EmptyTypes;
    }

    /// <inheritdoc />
    public virtual Type[] GetDependedTypes()
    {
        return DependedTypes;
    }
}
