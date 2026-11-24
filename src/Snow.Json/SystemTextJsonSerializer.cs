using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Json;

/// <summary>
/// 基于 System.Text.Json 的实现: 框架默认选择, 本包内直接顶替静默实现
/// <para>默认约定: 小驼峰、忽略 null、枚举按小驼峰字符串、允许注释与尾随逗号; 公共开关(缩进 / 忽略 null / 允许注释)在 <c>Json</c> 配置节的 SnowJsonOptions 里。</para>
/// <para>要用 Newtonsoft.Json 就装 Snow.Json.Newtonsoft, 后者以 ReplaceServices 顶替本实现。</para>
/// </summary>
[SingletonService(ReplaceServices = true)]
[ExposeServices(typeof(ISnowJsonSerializer))]
public sealed class SystemTextJsonSerializer : ISnowJsonSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="commonOptions">公共 JSON 选项</param>
    public SystemTextJsonSerializer(IOptions<SnowJsonOptions> commonOptions)
    {
        ArgumentNullException.ThrowIfNull(commonOptions);

        SnowJsonOptions common = commonOptions.Value;
        JsonSerializerOptions effective = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = common.WriteIndented,
        };

        effective.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        if (!common.IgnoreNullValues)
        {
            effective.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        }

        if (!common.AllowComments)
        {
            effective.ReadCommentHandling = JsonCommentHandling.Disallow;
            effective.AllowTrailingCommas = false;
        }

        _options = effective;
    }

    /// <summary>
    /// 生效的序列化选项(只读, 不要修改)
    /// </summary>
    public JsonSerializerOptions Options => _options;

    /// <inheritdoc />
    /// <param name="value">值</param>
    /// <param name="type">类型, 可为 null</param>
    public string Serialize(object? value, Type? type = null)
        => JsonSerializer.Serialize(value, type ?? value?.GetType() ?? typeof(object), _options);

    /// <inheritdoc />
    /// <param name="value">值</param>
    /// <param name="type">类型, 可为 null</param>
    public byte[] SerializeToUtf8Bytes(object? value, Type? type = null)
        => JsonSerializer.SerializeToUtf8Bytes(value, type ?? value?.GetType() ?? typeof(object), _options);

    /// <inheritdoc />
    /// <param name="json">JSON 字符串</param>
    public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);

    /// <inheritdoc />
    /// <param name="json">JSON 字符串</param>
    /// <param name="type">类型, 可为 null</param>
    public object? Deserialize(string json, Type type) => JsonSerializer.Deserialize(json, type, _options);

    /// <inheritdoc />
    /// <param name="stream">流</param>
    public T? Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, _options);
}
