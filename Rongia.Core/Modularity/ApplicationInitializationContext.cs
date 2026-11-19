using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rongia.DependencyInjection;

namespace Rongia.Modularity;

/// <summary>
/// 应用初始化上下文:容器构建之后传给模块的初始化钩子
/// </summary>
public sealed class ApplicationInitializationContext
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
    public ApplicationInitializationContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Items = new Dictionary<string, object?>();
    }
}
