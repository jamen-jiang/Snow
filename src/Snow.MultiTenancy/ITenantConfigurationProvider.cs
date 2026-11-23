namespace Snow.MultiTenancy;

/// <summary>
/// 租户配置提供器(对齐 ABP 的 <c>ITenantConfigurationProvider</c>)
/// <para>把「解析出资者给出的 id/名称」翻译成 <see cref="TenantConfiguration"/>; 解析出值但查不到时抛 <see cref="SnowTenantNotFoundException"/>。</para>
/// </summary>
public interface ITenantConfigurationProvider
{
    /// <summary>
    /// 取本次请求的租户配置
    /// </summary>
    /// <param name="saveResolveResult">是否把解析结果写进 <see cref="ITenantResolveResultAccessor"/>, 供后续代码复用</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户配置, 未解析出任何租户(即宿主)时为 null</returns>
    Task<TenantConfiguration?> GetAsync(bool saveResolveResult = false, CancellationToken cancellationToken = default);
}
