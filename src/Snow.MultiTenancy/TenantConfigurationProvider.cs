using Snow.DependencyInjection;

namespace Snow.MultiTenancy;

/// <summary>
/// 租户配置提供器的默认实现: 跑一遍解析器, 再拿结果去租户存储里查
/// </summary>
[TransientService]
public sealed class TenantConfigurationProvider : ITenantConfigurationProvider
{
    private readonly ITenantResolver _resolver;
    private readonly ITenantStore _store;
    private readonly ITenantResolveResultAccessor _resolveResultAccessor;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="resolver">租户解析器</param>
    /// <param name="store">租户存储</param>
    /// <param name="resolveResultAccessor">解析结果存取口子</param>
    public TenantConfigurationProvider(
        ITenantResolver resolver,
        ITenantStore store,
        ITenantResolveResultAccessor resolveResultAccessor)
    {
        _resolver = resolver;
        _store = store;
        _resolveResultAccessor = resolveResultAccessor;
    }

    /// <inheritdoc />
    /// <param name="saveResolveResult">是否缓存解析结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<TenantConfiguration?> GetAsync(bool saveResolveResult = false, CancellationToken cancellationToken = default)
    {
        TenantResolveResult result = await _resolver.ResolveTenantIdOrNameAsync().ConfigureAwait(false);

        if (saveResolveResult)
        {
            _resolveResultAccessor.Result = result;
        }

        if (!result.TenantId.HasValue && string.IsNullOrWhiteSpace(result.TenantName))
        {
            return null;
        }

        TenantConfiguration? tenant = await _store.FindAsync(result.TenantId, result.TenantName).ConfigureAwait(false);
        return tenant ?? throw new SnowTenantNotFoundException(result.TenantId, result.TenantName);
    }
}
