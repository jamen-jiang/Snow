namespace Snow.Json;

/// <summary>
/// JSON 选项: 与具体序列化库无关的公共约定
/// <para>库特有的细节(转换器、命名策略等)在实现包的选项里调, 例如 SnowSystemTextJsonOptions / SnowNewtonsoftJsonOptions。</para>
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
}
