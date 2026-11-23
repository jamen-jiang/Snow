namespace Snow.Modularity;

/// <summary>
/// 应用关停上下文:容器释放之前传给模块的关停钩子(对应 ABP 的 ApplicationShutdownContext)
/// </summary>
public sealed class ApplicationShutdownContext
{
    /// <summary>
    /// 关停阶段用的服务提供者,是一个独立作用域 —— 关停里可以解析 Scoped 服务
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">关停阶段的服务提供者</param>
    public ApplicationShutdownContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }
}
