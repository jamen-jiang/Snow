using System.Reflection;

namespace Rongia.Modularity;

/// <summary>
/// 为模块声明额外的程序集:模块需要扫描自身程序集之外的类型(例如另一层的实现)时使用,
/// 声明的程序集会进入 ModuleDescriptor.AllAssemblies
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AdditionalAssemblyAttribute : Attribute, IAdditionalModuleAssemblyProvider
{
    /// <summary>
    /// 声明中所用类型所在的程序集
    /// </summary>
    public Type[] TypesInAssemblies { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="typesInAssemblies">用来定位附加程序集的类型</param>
    public AdditionalAssemblyAttribute(params Type[]? typesInAssemblies)
    {
        TypesInAssemblies = typesInAssemblies ?? Type.EmptyTypes;
    }

    /// <inheritdoc />
    public Assembly[] GetAssemblies()
    {
        return TypesInAssemblies.Select(type => type.Assembly).Distinct().ToArray();
    }
}
