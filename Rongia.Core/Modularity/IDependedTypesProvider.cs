using System.Reflection;

namespace Rongia.Modularity;

/// <summary>
/// 声明依赖类型的提供者(对应 ABP 的 IDependedTypesProvider)
/// </summary>
public interface IDependedTypesProvider
{
    /// <summary>
    /// 取依赖的类型集合
    /// </summary>
    /// <returns>依赖的类型</returns>
    Type[] GetDependedTypes();
}
