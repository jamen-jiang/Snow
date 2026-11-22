using Snow.DependencyInjection;

namespace Snow.MultiTenancy;

/// <summary>
/// 租户解析结果的默认实现: 作用域内共享一份
/// </summary>
[ScopedService]
public sealed class TenantResolveResultAccessor : ITenantResolveResultAccessor
{
    /// <inheritdoc />
    public TenantResolveResult? Result { get; set; }
}
