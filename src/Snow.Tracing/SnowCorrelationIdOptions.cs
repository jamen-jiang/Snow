namespace Snow.Tracing;

/// <summary>
/// 链路标识选项(对应 ABP 的 <c>AbpCorrelationIdOptions</c>)
/// <para>控制从哪个请求头读取链路标识, 以及是否把它写回响应头。</para>
/// </summary>
public class SnowCorrelationIdOptions
{
    /// <summary>
    /// 传递链路标识的请求头/响应头名称
    /// </summary>
    public string HttpHeaderName { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// 是否把链路标识写回响应头
    /// </summary>
    public bool SetResponseHeader { get; set; } = true;
}
