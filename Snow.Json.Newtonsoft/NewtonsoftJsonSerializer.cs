using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Snow.DependencyInjection;

namespace Snow.Json.Newtonsoft;

/// <summary>
/// 基于 Newtonsoft.Json 的实现
/// <para>构造时把公共选项(缩进 / 忽略 null / 允许注释)合并进库设置, 运行期只读。</para>
/// </summary>
[SingletonService(ReplaceServices = true)]
[ExposeServices(typeof(ISnowJsonSerializer))]
public sealed class NewtonsoftJsonSerializer : ISnowJsonSerializer
{
    private readonly JsonSerializerSettings _settings;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">Newtonsoft.Json 选项</param>
    /// <param name="commonOptions">公共 JSON 选项</param>
    public NewtonsoftJsonSerializer(IOptions<SnowNewtonsoftJsonOptions> options, IOptions<SnowJsonOptions> commonOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(commonOptions);

        SnowJsonOptions common = commonOptions.Value;
        JsonSerializerSettings effective = new(options.Value.Settings)
        {
            Formatting = common.WriteIndented ? Formatting.Indented : Formatting.None,
            NullValueHandling = common.IgnoreNullValues ? NullValueHandling.Ignore : NullValueHandling.Include,
        };

        if (common.AllowComments)
        {
            effective.DateParseHandling = DateParseHandling.DateTime;
        }

        _settings = effective;
    }

    /// <summary>
    /// 生效的序列化设置(只读, 不要修改)
    /// </summary>
    public JsonSerializerSettings Settings => _settings;

    /// <inheritdoc />
    public string Serialize(object? value, Type? type = null)
        => JsonConvert.SerializeObject(value, type ?? value?.GetType() ?? typeof(object), _settings);

    /// <inheritdoc />
    public byte[] SerializeToUtf8Bytes(object? value, Type? type = null)
        => Encoding.UTF8.GetBytes(Serialize(value, type));

    /// <inheritdoc />
    public T? Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, _settings);

    /// <inheritdoc />
    public object? Deserialize(string json, Type type) => JsonConvert.DeserializeObject(json, type, _settings);

    /// <inheritdoc />
    public T? Deserialize<T>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return Deserialize<T>(reader.ReadToEnd());
    }
}
