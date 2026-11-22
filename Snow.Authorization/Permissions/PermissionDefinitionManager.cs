using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限定义管理器默认实现: 首次访问时汇总全部定义提供器, 之后常驻内存
/// </summary>
[SingletonService]
public sealed class PermissionDefinitionManager : IPermissionDefinitionManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PermissionOptions _options;
    private readonly Lazy<DefinitionSnapshot> _snapshot;

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

        // Lazy 自带线程安全的"只执行一次", 比手写双检锁少一层判断
        _snapshot = new Lazy<DefinitionSnapshot>(Build, LazyThreadSafetyMode.ExecutionAndPublication);
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
        => Task.FromResult<IReadOnlyList<PermissionDefinition>>(_snapshot.Value.Flattened);

    /// <inheritdoc />
    public Task<IReadOnlyList<PermissionGroupDefinition>> GetGroupsAsync()
        => Task.FromResult<IReadOnlyList<PermissionGroupDefinition>>(_snapshot.Value.Groups);

    private PermissionDefinition? GetOrNull(string name)
        => _snapshot.Value.Permissions.TryGetValue(name, out PermissionDefinition? permission) ? permission : null;

    /// <summary>
    /// 跑一遍全部定义提供器, 把分组树展平成字典与列表
    /// </summary>
    /// <returns>展平后的快照</returns>
    private DefinitionSnapshot Build()
    {
        PermissionDefinitionContext context = new(_serviceProvider);

        foreach (Type providerType in _options.DefinitionProviders)
        {
            if (_serviceProvider.GetService(providerType) is not IPermissionDefinitionProvider provider)
            {
                throw new InvalidOperationException(
                    $"权限定义提供器 {providerType.FullName} 没有在容器中注册, 或没有实现 {nameof(IPermissionDefinitionProvider)}。" +
                    $"让它标 [TransientService], 或用 services.AddTransient({providerType.Name}) 显式登记。");
            }

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

        return new DefinitionSnapshot(groups, flattened, permissions);
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

    /// <summary>
    /// 展平结果: 分组、权限列表与按名索引
    /// </summary>
    /// <param name="Groups">分组</param>
    /// <param name="Flattened">展平后的权限列表</param>
    /// <param name="Permissions">权限名到定义的索引</param>
    private sealed record DefinitionSnapshot(
        List<PermissionGroupDefinition> Groups,
        List<PermissionDefinition> Flattened,
        Dictionary<string, PermissionDefinition> Permissions);
}
