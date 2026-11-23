namespace Snow.Authentication;

/// <summary>
/// 登录来源信息: 写进会话, 让"在线设备列表"能显示是哪台机器
/// <para>只作展示与人工挑选下线目标, 不参与任何校验。</para>
/// </summary>
public sealed class TokenClientInfo
{
    /// <summary>
    /// 来源IP
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 来源设备标识(通常是 User-Agent)
    /// </summary>
    public string? UserAgent { get; set; }
}
