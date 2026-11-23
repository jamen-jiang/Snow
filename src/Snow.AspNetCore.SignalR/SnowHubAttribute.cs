namespace Snow.AspNetCore.SignalR;

/// <summary>
/// 标注一个 Hub 的端点路径, 不写则按 "/signalr/{类名去 Hub}" 生成
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SnowHubAttribute : Attribute
{
    /// <summary>
    /// 创建标注
    /// </summary>
    /// <param name="path">端点路径, 省略则按类名推导</param>
    public SnowHubAttribute(string? path = null)
    {
        Path = path;
    }

    /// <summary>
    /// 端点路径
    /// </summary>
    public string? Path { get; }
}
