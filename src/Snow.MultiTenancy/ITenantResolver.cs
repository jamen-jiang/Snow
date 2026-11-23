namespace Snow.MultiTenancy;

/// <summary>
/// 租户解析器(对齐 ABP 的 <c>ITenantResolver</c>): 按顺序跑出资者, 返回首个结果
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// 解析当前请求的租户
    /// </summary>
    /// <returns>解析结果, 未解析出时两个字段都为空</returns>
    Task<TenantResolveResult> ResolveTenantIdOrNameAsync();
}
