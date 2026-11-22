namespace Snow.MultiTenancy;

/// <summary>
/// 租户解析上下文的默认实现
/// </summary>
public sealed class TenantResolveContext : ITenantResolveContext
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public TenantResolveContext(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        ServiceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public string? TenantIdOrName { get; set; }

    /// <inheritdoc />
    public bool Handled { get; set; }
}
