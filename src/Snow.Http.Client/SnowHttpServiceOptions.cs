using System.Text.Json;

namespace Snow.Http.Client;

/// <summary>
/// 声明式 HTTP 接口的选项: 目前只有序列化设置(默认 Web 风格: camelCase 属性名、大小写不敏感)
/// <para>需要自定义时 <c>services.Configure&lt;SnowHttpServiceOptions&gt;(options =&gt; options.JsonSerializerOptions = new(...));</c></para>
/// </summary>
public sealed class SnowHttpServiceOptions
{
    /// <summary>
    /// 默认实例
    /// </summary>
    internal static readonly SnowHttpServiceOptions Default = new();

    /// <summary>
    /// 序列化设置
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web);
}
