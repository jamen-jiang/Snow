using Snow.DependencyInjection;
using Snow.Settings;

namespace Snow.MultiTenancy;

/// <summary>
/// 租户设置提供器: 按当前租户从设置存储里取值, 名字 "T"
/// <para>排序在全局提供器 "G" 之后(名字更大的排后面), 因此租户设置优先于全局设置 —— 与 ABP 一致。</para>
/// <para>不在租户上下文里(宿主请求)时直接返回 null, 把决定权交给后面的提供器。</para>
/// </summary>
[SingletonService]
public sealed class TenantSettingValueProvider : ISettingValueProvider
{
    /// <summary>
    /// 提供器名, 同时是设置存储里的 providerName
    /// </summary>
    public const string ProviderName = "T";

    private readonly ISettingStore _store;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="store">设置存储</param>
    /// <param name="currentTenant">当前租户</param>
    public TenantSettingValueProvider(ISettingStore store, ICurrentTenant currentTenant)
    {
        _store = store;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public Task<string?> GetOrNullAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Guid? tenantId = _currentTenant.Id;
        if (!tenantId.HasValue)
        {
            return Task.FromResult<string?>(null);
        }

        return _store.GetOrNullAsync(definition.Name, ProviderName, tenantId.Value.ToString(), cancellationToken);
    }
}
