using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限定义管理器默认实现: 首次访问时汇总全部定义提供器, 之后常驻内存
/// </summary>
public sealed class PermissionDefinitionManager : IPermissionDefinitionManager, ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PermissionOptions _options;
    private readonly object _sync = new();

    private Dictionary<string, PermissionDefinition>? _permissions;
    private List<PermissionDefinition>? _flattened;
    private List<PermissionGroupDefinition>? _groups;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="options">权限选项</param>
    public PermissionDefinitionManager(IServiceProvider serviceProvider, IOptions<PermissionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<PermissionDefinition> GetAsync(string name)
        => Task.FromResult(GetOrNull(name) ?? throw new InvalidOperationException($"未定义的权限: {name}"));

    /// <inheritdoc />
    public Task<PermissionDefinition?> GetOrNullAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Task.FromResult(GetOrNull(name));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PermissionDefinition>> GetPermissionsAsync()
    {
        EnsureInitialized();
        return Task.FromResult<IReadOnlyList<PermissionDefinition>>(_flattened!);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PermissionGroupDefinition>> GetGroupsAsync()
    {
        EnsureInitialized();
        return Task.FromResult<IReadOnlyList<PermissionGroupDefinition>>(_groups!);
    }

    private PermissionDefinition? GetOrNull(string name)
    {
        EnsureInitialized();
        return _permissions!.TryGetValue(name, out PermissionDefinition? permission) ? permission : null;
    }

    private void EnsureInitialized()
    {
        if (_permissions != null)
        {
            return;
        }

        lock (_sync)
        {
            if (_permissions != null)
            {
                return;
            }

            PermissionDefinitionContext context = new(_serviceProvider);

            foreach (Type providerType in _options.DefinitionProviders)
            {
                IPermissionDefinitionProvider provider =
                    (IPermissionDefinitionProvider)_serviceProvider.GetRequiredService(providerType);

                provider.PreDefine(context);
                provider.Define(context);
                provider.PostDefine(context);
            }

            List<PermissionGroupDefinition> groups = [.. context.Groups.Values];
            List<PermissionDefinition> flattened = [];
            Dictionary<string, PermissionDefinition> permissions = new(StringComparer.Ordinal);

            foreach (PermissionGroupDefinition group in groups)
            {
                foreach (PermissionDefinition permission in group.Permissions)
                {
                    AddPermission(permissions, flattened, permission);
                }
            }

            _groups = groups;
            _flattened = flattened;
            _permissions = permissions;
        }
    }

    private static void AddPermission(
        Dictionary<string, PermissionDefinition> permissions,
        List<PermissionDefinition> flattened,
        PermissionDefinition permission)
    {
        if (!permissions.TryAdd(permission.Name, permission))
        {
            throw new InvalidOperationException($"重复的权限名: {permission.Name}");
        }

        flattened.Add(permission);

        foreach (PermissionDefinition child in permission.Children)
        {
            AddPermission(permissions, flattened, child);
        }
    }
}
