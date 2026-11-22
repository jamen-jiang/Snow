namespace Snow.AspNetCore.SignalR;

/// <summary>
/// SignalR 选项
/// </summary>
public sealed class SnowSignalROptions
{
    /// <summary>
    /// 是否启用, 关闭后模块只登记服务不映射端点
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否自动扫描各模块程序集里的 Hub 派生类
    /// </summary>
    public bool AutoDiscoverHubs { get; set; } = true;

    /// <summary>
    /// 是否把异常细节回给客户端, 生产环境默认关闭
    /// </summary>
    public bool EnableDetailedErrors { get; set; }

    /// <summary>
    /// 单条消息的最大字节数, 为空则用框架默认值
    /// </summary>
    public long? MaximumReceiveMessageSize { get; set; }
}
