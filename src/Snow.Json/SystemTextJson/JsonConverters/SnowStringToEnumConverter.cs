using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snow.Json.SystemTextJson.JsonConverters;

/// <summary>
/// 枚举字符串转换器(对应 ABP 的 <c>AbpStringToEnumConverter</c>)。
/// </summary>
/// <typeparam name="T">枚举类型</typeparam>
public class SnowStringToEnumConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    private readonly JsonStringEnumConverter _innerJsonStringEnumConverter;
    private JsonSerializerOptions? _readJsonSerializerOptions;
    private JsonSerializerOptions? _writeJsonSerializerOptions;

    /// <summary>
    /// 使用默认命名策略并允许整数值。
    /// </summary>
    public SnowStringToEnumConverter()
        : this(namingPolicy: null, allowIntegerValues: true)
    {
    }

    /// <summary>
    /// 构造器。
    /// </summary>
    /// <param name="namingPolicy">枚举名称命名策略, 可为 null</param>
    /// <param name="allowIntegerValues">是否允许用整数表示枚举</param>
    public SnowStringToEnumConverter(JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true)
    {
        _innerJsonStringEnumConverter = new JsonStringEnumConverter(namingPolicy, allowIntegerValues);
    }

    /// <inheritdoc />
    /// <param name="typeToConvert">待转换类型</param>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    /// <param name="reader">JSON 读取器</param>
    /// <param name="typeToConvert">待转换类型</param>
    /// <param name="options">序列化选项</param>
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        _readJsonSerializerOptions ??= JsonSerializerOptionsHelper.Create(options, x =>
                x == this ||
                x.GetType() == typeof(SnowStringToEnumFactory),
            _innerJsonStringEnumConverter.CreateConverter(typeToConvert, options));

        return JsonSerializer.Deserialize<T>(ref reader, _readJsonSerializerOptions);
    }

    /// <inheritdoc />
    /// <param name="writer">JSON 写入器</param>
    /// <param name="value">枚举值</param>
    /// <param name="options">序列化选项</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        _writeJsonSerializerOptions ??= JsonSerializerOptionsHelper.Create(options, x =>
                x == this ||
                x.GetType() == typeof(SnowStringToEnumFactory));

        JsonSerializer.Serialize(writer, value, _writeJsonSerializerOptions);
    }

    /// <inheritdoc />
    /// <param name="reader">JSON 读取器</param>
    /// <param name="typeToConvert">待转换类型</param>
    /// <param name="options">序列化选项</param>
    public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return (T)Enum.Parse(typeToConvert, reader.GetString()!);
    }

    /// <inheritdoc />
    /// <param name="writer">JSON 写入器</param>
    /// <param name="value">枚举值</param>
    /// <param name="options">序列化选项</param>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(Enum.GetName(typeof(T), value)!);
    }
}
