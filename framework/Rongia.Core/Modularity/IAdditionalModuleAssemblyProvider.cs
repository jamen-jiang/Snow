using System.Reflection;

namespace Rongia.Modularity;

/// <summary>
/// 附加程序集提供者:实现该接口的特性可以为模块声明额外的程序集
/// </summary>
public interface IAdditionalModuleAssemblyProvider
{
    /// <summary>
    /// 取声明的附加程序集
    /// </summary>
    /// <returns>程序集列表</returns>
    Assembly[] GetAssemblies();
}
