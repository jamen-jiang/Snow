using Microsoft.Extensions.DependencyInjection;

namespace Snow.Application.Services;

/// <summary>
/// 应用服务基类(对应 ABP 的 ApplicationService)
/// <para>只提供容器访问, 不掺入任何领域或数据访问依赖: 需要什么自行从容器取。</para>
/// </summary>
public abstract class ApplicationService : IApplicationService
{
    /// <summary>
    /// 构造器: 容器由约定注册与 Web 层的服务型控制器激活器注入
    /// </summary>
    /// <param name="serviceProvider">当前作用域的容器</param>
    protected ApplicationService(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// 当前作用域的容器
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 按类型取必需的服务, 取不到直接抛异常
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    protected T Lazy<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();
}
