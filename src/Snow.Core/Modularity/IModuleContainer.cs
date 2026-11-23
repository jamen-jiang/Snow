namespace Snow.Modularity;

/// <summary>
/// 模块容器(对应 ABP 的 IModuleContainer):保存本次应用加载的全部模块及其依赖顺序。
/// 各包用它做程序集发现(<c>Modules.SelectMany(module => module.AllAssemblies)</c>)。
/// </summary>
public interface IModuleContainer
{
    /// <summary>
    /// 按加载顺序排列的模块列表,被依赖的模块排在前
    /// </summary>
    IReadOnlyList<SnowModuleDescriptor> Modules { get; }
}
