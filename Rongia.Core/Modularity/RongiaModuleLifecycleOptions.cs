namespace Rongia.Modularity;

/// <summary>
/// 模块生命周期配置(对应 ABP 的 AbpModuleLifecycleOptions):
/// 贡献者按加入顺序执行,顺序即阶段顺序
/// </summary>
public sealed class RongiaModuleLifecycleOptions
{
    /// <summary>
    /// 生命周期贡献者类型列表,按加入顺序驱动
    /// </summary>
    public List<Type> Contributors { get; } = new List<Type>();
}
