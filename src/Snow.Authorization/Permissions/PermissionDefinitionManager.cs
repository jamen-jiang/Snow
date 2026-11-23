using Snow.DependencyInjection;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限定义管理器: 汇总全部 <see cref="PermissionDefinitionProvider"/> 的定义并按名索引
/// <para>第一次访问时构建, 之后走缓存: 定义是启动期数据, 不随请求变化。</para>
/// </summary>
[SingletonService]
public sealed class PermissionDefinitionManager
{
    private readonly IEnumerable<PermissionDefinitionProvider> _providers;
    private readonly Lazy<Snapshot> _snapshot;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="providers">全部定义提供器</param>
    public PermissionDefinitionManager(IEnumerable<PermissionDefinitionProvider> providers)
    {
        _providers = providers;
        _snapshot = new Lazy<Snapshot>(Build, isThreadSafe: true);
    }

    /// <summary>
    /// 按名取权限定义, 不存在返回 null
    /// </summary>
    /// <param name="name">权限名</param>
    /// <returns>权限定义或 null</returns>
    public PermissionDefinition? GetOrNull(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _snapshot.Value.ByName.TryGetValue(name, out PermissionDefinition? definition) ? definition : null;
    }

    /// <summary>
    /// 按名取权限定义, 不存在抛异常
    /// </summary>
    /// <param name="name">权限名</param>
    /// <returns>权限定义</returns>
    /// <exception cref="KeyNotFoundException">未定义的权限</exception>
    public PermissionDefinition Get(string name)
        => GetOrNull(name) ?? throw new KeyNotFoundException("未定义的权限: " + name);

    /// <summary>
    /// 全部权限(含子权限), 按定义顺序
    /// </summary>
    /// <returns>权限定义集合</returns>
    public IReadOnlyList<PermissionDefinition> GetAll() => _snapshot.Value.All;

    /// <summary>
    /// 全部分组
    /// </summary>
    /// <returns>分组定义集合</returns>
    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _snapshot.Value.Groups;

    private Snapshot Build()
    {
        PermissionDefinitionContext context = new();

        foreach (PermissionDefinitionProvider provider in _providers)
        {
            provider.Define(context);
        }

        Dictionary<string, PermissionDefinition> byName = new(StringComparer.Ordinal);
        List<PermissionGroupDefinition> groups = [.. context.Groups];

        foreach (PermissionGroupDefinition group in groups)
        {
            group.Collect(byName);
        }

        return new Snapshot(byName, groups, [.. byName.Values]);
    }

    private sealed record Snapshot(
        Dictionary<string, PermissionDefinition> ByName,
        IReadOnlyList<PermissionGroupDefinition> Groups,
        IReadOnlyList<PermissionDefinition> All);
}
