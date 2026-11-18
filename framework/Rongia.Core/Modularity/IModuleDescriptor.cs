using System.Reflection;

namespace Rongia.Modularity;

/// <summary>
/// 模块描述信息(对应 ABP 的 IAbpModuleDescriptor)
/// </summary>
public interface IModuleDescriptor
{
    /// <summary>
    /// 模块类型
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// 定义模块类型的主程序集
    /// </summary>
    Assembly Assembly { get; }

    /// <summary>
    /// 模块相关的全部程序集:主程序集加上模块类型上 AdditionalAssemblyAttribute 声明的附加程序集
    /// </summary>
    Assembly[] AllAssemblies { get; }

    /// <summary>
    /// 模块实例(单例)
    /// </summary>
    IModule Instance { get; }

    /// <summary>
    /// 该模块是否以插件方式加载
    /// </summary>
    bool IsLoadedAsPlugIn { get; }

    /// <summary>
    /// 该模块直接依赖的模块,由 DependsOnAttribute 声明
    /// </summary>
    IReadOnlyList<IModuleDescriptor> Dependencies { get; }
}
