namespace Snow.MultiTenancy;

/// <summary>
/// 本次请求的租户解析结果存取口子(对齐 ABP 的 <c>ITenantResolveResultAccessor</c>)
/// </summary>
public interface ITenantResolveResultAccessor
{
    /// <summary>
    /// 解析结果, 未解析为 null
    /// </summary>
    TenantResolveResult? Result { get; set; }
}
