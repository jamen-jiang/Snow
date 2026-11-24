using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Snow.DependencyInjection;

namespace Snow.Json.Newtonsoft;

/// <summary>
/// 基于 Newtonsoft.Json 的实现
/// <para>默认约定尽量与 System.Text.Json 实现对齐(小驼峰、忽略 null、枚举小驼峰字符串、ISO 日期); 公共开关(缩进 / 忽略 null / 允许注释)在 <c>Json</c> 配置节的 SnowJsonOptions 里。</para>
/// </summary>
[SingletonService(ReplaceServices = true)]
[ExposeServices(typeof(ISnowJsonSerializer))]
public sealed class NewtonsoftJsonSerializer : ISnowJsonSerializer
{
    private readonly JsonSerializerSettings _settings;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="commonOptions">公共 JSON 选项</param>
    public NewtonsoftJsonSerializer(IOptions<SnowJsonOptions> commonOptions)
    {
        ArgumentNullException.ThrowIfNull(commonOptions);

        SnowJsonOptions common = commonOptions.Value;
        JsonSerializerSettings effective = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = common.IgnoreNullValues ? NullValueHandling.Ignore : NullValueHandling.Include,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Culture = CultureInfo.InvariantCulture,
            Formatting = common.WriteIndented ? Formatting.Indented : Formatting.None,
        };

        effective.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));

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
    /// <param name="value">值</param>
    /// <param name="type">类型, 可为 null</param>
    public string Serialize(object? value, Type? type = null)
        => JsonConvert.SerializeObject(value, type ?? value?.GetType() ?? typeof(object), _settings);

    /// <inheritdoc />
    /// <param name="value">值</param>
    /// <param name="type">类型, 可为 null</param>
    public byte[] SerializeToUtf8Bytes(object? value, Type? type = null)
        => Encoding.UTF8.GetBytes(Serialize(value, type));

    /// <inheritdoc />
    /// <param name="json">JSON 字符串</param>
    public T? Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, _settings);

    /// <inheritdoc />
    /// <param name="json">JSON 字符串</param>
    /// <param name="type">类型, 可为 null</param>
    public object? Deserialize(string json, Type type) => JsonConvert.DeserializeObject(json, type, _settings);

    /// <inheritdoc />
    /// <param name="stream">流</param>
    public T? Deserialize<T>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return Deserialize<T>(reader.ReadToEnd());
    }
}
