using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.DependencyInjection;

/// <summary>
/// 约定注册器:负责把某个程序集或某批类型按约定登记进容器(对应 ABP IConventionalRegistrar)
/// </summary>
public interface IConventionalRegistrar
{
    /// <summary>
    /// 扫描整个程序集并注册
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assembly">目标程序集</param>
    void AddAssembly(IServiceCollection services, Assembly assembly);

    /// <summary>
    /// 注册指定的多个类型
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="types">类型列表</param>
    void AddTypes(IServiceCollection services, params Type[] types);

    /// <summary>
    /// 注册单个类型
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="type">类型</param>
    void AddType(IServiceCollection services, Type type);
}
