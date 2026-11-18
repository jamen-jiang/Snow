namespace Rongia.Modularity;

/// <summary>
/// 模块容器:保存本次应用加载的全部模块及其依赖顺序
/// </summary>
public interface IModuleContainer
{
    /// <summary>
    /// 按加载顺序排列的模块列表,被依赖的模块排在前
    /// </summary>
    IReadOnlyList<ModuleDescriptor> Modules { get; }

    /// <summary>
    /// 按类型查找模块描述信息
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <returns>找到时返回描述信息,否则返回 null</returns>
    ModuleDescriptor? Find(Type moduleType);
}
