using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Json;

/// <summary>
/// 基于 System.Text.Json 的实现: 框架默认选择, 本包内直接顶替静默实现
/// <para>构造时把公共选项(缩进 / 忽略 null / 允许注释)合并进库选项, 运行期只读。</para>
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
    /// <param name="options">System.Text.Json 选项</param>
    /// <param name="commonOptions">公共 JSON 选项</param>
    public SystemTextJsonSerializer(IOptions<SnowSystemTextJsonOptions> options, IOptions<SnowJsonOptions> commonOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(commonOptions);

        SnowJsonOptions common = commonOptions.Value;
        JsonSerializerOptions effective = new(options.Value.SerializerOptions)
        {
            WriteIndented = common.WriteIndented,
        };

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
    public string Serialize(object? value, Type? type = null)
        => JsonSerializer.Serialize(value, type ?? value?.GetType() ?? typeof(object), _options);

    /// <inheritdoc />
    public byte[] SerializeToUtf8Bytes(object? value, Type? type = null)
        => JsonSerializer.SerializeToUtf8Bytes(value, type ?? value?.GetType() ?? typeof(object), _options);

    /// <inheritdoc />
    public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);

    /// <inheritdoc />
    public object? Deserialize(string json, Type type) => JsonSerializer.Deserialize(json, type, _options);

    /// <inheritdoc />
    public T? Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, _options);
}
