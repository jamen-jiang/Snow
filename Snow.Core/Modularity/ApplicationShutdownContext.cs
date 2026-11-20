using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.Modularity;

/// <summary>
/// 应用关闭上下文:按加载顺序的逆序传给模块的关闭钩子
/// </summary>
public sealed class ApplicationShutdownContext
{
    /// <summary>
    /// 已构建完成的服务提供者
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 跨模块共享的状态袋
    /// </summary>
    public IDictionary<string, object?> Items { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public ApplicationShutdownContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Items = new Dictionary<string, object?>();
    }
}
