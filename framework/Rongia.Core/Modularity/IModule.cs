using System.Threading.Tasks;

namespace Rongia.Modularity;

/// <summary>
/// 模块契约。加载器只认这个接口,不要求继承 <see cref="Module"/>:
/// 自定义基类或直接实现本接口的类同样可以被 [DependsOn] 组织起来。
/// <para>异步与同步两个版本同时存在,宿主可以任选其一调用;实现方通常只在其中一个里写逻辑。</para>
/// </summary>
public interface IModule
{
    /// <summary>
    /// 服务注册阶段的异步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    Task ConfigureServicesAsync(ServiceConfigurationContext context);

    /// <summary>
    /// 服务注册阶段:约定扫描本模块程序集之后调用,
    /// 因此这里显式登记的服务会优先于约定注册生效。
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    void ConfigureServices(ServiceConfigurationContext context);
}
