namespace Snow.AspNetCore.HealthChecks;

/// <summary>
/// 检查项登记表: 收集宿主在 <c>AddApplication</c> 之前登记的检查类型
/// </summary>
public sealed class SnowHealthCheckRegistry
{
    private readonly List<SnowHealthCheckRegistration> _items = [];

    /// <summary>
    /// 已登记的检查项
    /// </summary>
    public IReadOnlyList<SnowHealthCheckRegistration> Items => _items;

    /// <summary>
    /// 登记一个检查项
    /// </summary>
    /// <param name="name">检查项名称</param>
    /// <param name="checkType">实现 IHealthCheck 的类型</param>
    /// <param name="tags">标签</param>
    public void Add(string name, Type checkType, string[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(checkType);

        _items.Add(new SnowHealthCheckRegistration(name, checkType, tags));
    }
}

/// <summary>
/// 一个检查项的登记信息
/// </summary>
/// <param name="Name">检查项名称</param>
/// <param name="CheckType">实现 IHealthCheck 的类型</param>
/// <param name="Tags">标签</param>
public sealed record SnowHealthCheckRegistration(string Name, Type CheckType, string[] Tags);
