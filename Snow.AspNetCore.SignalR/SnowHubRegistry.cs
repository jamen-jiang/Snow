namespace Snow.AspNetCore.SignalR;

/// <summary>
/// Hub 登记表: 收集自动扫描与显式登记的 Hub
/// </summary>
public sealed class SnowHubRegistry
{
    private readonly List<SnowHubRegistration> _items = [];

    /// <summary>
    /// 已登记的 Hub
    /// </summary>
    public IReadOnlyList<SnowHubRegistration> Items => _items;

    /// <summary>
    /// 登记一个 Hub, 同一类型只保留首次登记
    /// </summary>
    /// <param name="hubType">Hub 类型</param>
    /// <param name="path">端点路径</param>
    public void Add(Type hubType, string path)
    {
        ArgumentNullException.ThrowIfNull(hubType);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (_items.Any(item => item.HubType == hubType))
        {
            return;
        }

        _items.Add(new SnowHubRegistration(hubType, path));
    }
}

/// <summary>
/// 一个 Hub 的登记信息
/// </summary>
/// <param name="HubType">Hub 类型</param>
/// <param name="Path">端点路径</param>
public sealed record SnowHubRegistration(Type HubType, string Path);
