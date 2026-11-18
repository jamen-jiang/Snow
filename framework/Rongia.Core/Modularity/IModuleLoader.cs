using Microsoft.Extensions.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 模块加载器(对应 ABP 的 IModuleLoader)
/// </summary>
public interface IModuleLoader
{
    /// <summary>
    /// 加载启动模块及其全部依赖
    /// </summary>
    /// <param name="services">服务集合,模块实例会被注册进去</param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns>按依赖顺序排列的模块描述信息,启动模块排在最后</returns>
    IModuleDescriptor[] LoadModules(IServiceCollection services, Type startupModuleType);
}
