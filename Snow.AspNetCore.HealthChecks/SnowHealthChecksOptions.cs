namespace Snow.AspNetCore.HealthChecks;

/// <summary>
/// 健康检查端点选项
/// </summary>
public sealed class SnowHealthChecksOptions
{
    /// <summary>
    /// 健康端点的路由, 默认 "/health"
    /// </summary>
    public string Path { get; set; } = "/health";

    /// <summary>
    /// 是否要求已登录才能访问, 默认匿名可访问
    /// </summary>
    public bool RequireAuthorization { get; set; }

    /// <summary>
    /// 是否在响应里带上每个检查项的明细
    /// </summary>
    public bool IncludeDetails { get; set; } = true;
}
