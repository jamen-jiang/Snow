namespace Rongia.Modularity;

/// <summary>
/// 模块容器(对应 ABP 的 IModuleContainer):保存本次应用加载的全部模块及其依赖顺序
/// </summary>
public interface IModuleContainer
{
    /// <summary>
    /// 按加载顺序排列的模块列表,被依赖的模块排在前
    /// </summary>
    IReadOnlyList<ModuleDescriptor> Modules { get; }

    /// <summary>
    /// 模块类型到模块描述信息的索引,与 <see cref="Modules"/> 内容一致。
    /// 候选来源是加载器已建好的字典,容器不重建。
    /// </summary>
    IReadOnlyDictionary<Type, ModuleDescriptor> ModulesByType { get; }

    /// <summary>
    /// 按模块类型查找模块描述信息
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <returns>找到的模块描述信息;未加载该模块时返回 null</returns>
    ModuleDescriptor? Find(Type moduleType);
}
