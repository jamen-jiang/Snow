using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snow.Json;

/// <summary>
/// System.Text.Json 选项: 默认值即框架统一约定(小驼峰、忽略 null、枚举按小驼峰字符串)
/// <para>公共开关(缩进/忽略 null/注释)在 <c>Json</c> 配置节的 SnowJsonOptions 里, 这里只管库特有细节。</para>
/// </summary>
public sealed class SnowSystemTextJsonOptions
{
    /// <summary>
    /// 构造器: 建好带默认值的序列化选项
    /// </summary>
    public SnowSystemTextJsonOptions()
    {
        SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    /// <summary>
    /// 序列化选项: 直接增删转换器或改属性名策略
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; }
}
