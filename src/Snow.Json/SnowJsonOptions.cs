namespace Snow.Json;

/// <summary>
/// JSON 选项: 与具体序列化库无关的公共约定
/// <para>库特有的细节(转换器、命名策略等)写在各实现包的序列化器里, 不再单独暴露选项。</para>
/// </summary>
public sealed class SnowJsonOptions
{
    /// <summary>
    /// 是否缩进输出, 默认不缩进
    /// </summary>
    public bool WriteIndented { get; set; }

    /// <summary>
    /// 是否忽略值为 null 的属性, 默认忽略
    /// </summary>
    public bool IgnoreNullValues { get; set; } = true;

    /// <summary>
    /// 是否允许注释与尾随逗号, 默认允许
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// 输入日期时间格式集合, 为空时按 ISO/系统默认解析
    /// </summary>
    public List<string> InputDateTimeFormats { get; set; } = new();

    /// <summary>
    /// 输出日期时间格式, 为空时输出 ISO 格式
    /// </summary>
    public string? OutputDateTimeFormat { get; set; }
}
