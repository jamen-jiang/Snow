namespace Snow.MultiTenancy;

/// <summary>
/// 租户解析上下文(对齐 ABP 的 <c>ITenantResolveContext</c>)
/// <para>出资者往 <see cref="TenantIdOrName"/> 写候选值并置 <see cref="Handled"/>, 后续出资者不再执行。</para>
/// </summary>
public interface ITenantResolveContext
{
    /// <summary>
    /// 解析过程可用的服务提供者
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 候选的租户 id 或名称: 能被 <see cref="Guid"/> 解析当 id, 否则当名称
    /// </summary>
    string? TenantIdOrName { get; set; }

    /// <summary>
    /// 是否已有出资者给出了结果
    /// </summary>
    bool Handled { get; set; }
}
