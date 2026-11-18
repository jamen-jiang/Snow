namespace Rongia.Modularity;

/// <summary>
/// 模块容器(对应 ABP 的 IModuleContainer):保存本次应用加载的全部模块及其依赖顺序
/// </summary>
public interface IModuleContainer
{
    /// <summary>
    /// 按加载顺序排列的模块列表,被依赖的模块排在前
    /// </summary>
    IReadOnlyList<IModuleDescriptor> Modules { get; }
}
