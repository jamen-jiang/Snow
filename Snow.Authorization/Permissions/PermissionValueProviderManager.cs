using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限值提供器管理器默认实现: 按 <see cref="PermissionOptions.ValueProviders"/> 惰性实例化并校验重名
/// </summary>
public sealed class PermissionValueProviderManager : IPermissionValueProviderManager, ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PermissionOptions _options;
    private readonly Lazy<IReadOnlyList<IPermissionValueProvider>> _lazyProviders;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="options">权限选项</param>
    public PermissionValueProviderManager(IServiceProvider serviceProvider, IOptions<PermissionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _options = options.Value;
        _lazyProviders = new Lazy<IReadOnlyList<IPermissionValueProvider>>(CreateProviders, isThreadSafe: true);
    }

    /// <inheritdoc />
    public IReadOnlyList<IPermissionValueProvider> ValueProviders => _lazyProviders.Value;

    private IReadOnlyList<IPermissionValueProvider> CreateProviders()
    {
        List<IPermissionValueProvider> providers = [];

        foreach (Type providerType in _options.ValueProviders)
        {
            providers.Add((IPermissionValueProvider)_serviceProvider.GetRequiredService(providerType));
        }

        IGrouping<string, IPermissionValueProvider>? duplicated = providers
            .GroupBy(provider => provider.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicated != null)
        {
            throw new InvalidOperationException($"权限值提供器名重复: {duplicated.Key}");
        }

        return providers;
    }
}
