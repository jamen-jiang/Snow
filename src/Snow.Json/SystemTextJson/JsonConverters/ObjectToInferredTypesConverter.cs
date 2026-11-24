using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snow.Json.SystemTextJson.JsonConverters;

/// <summary>
/// 把 object 按 JSON token 推断为具体类型的转换器(对应 ABP 的 <c>ObjectToInferredTypesConverter</c>)。
/// </summary>
public class ObjectToInferredTypesConverter : JsonConverter<object>
{
    /// <inheritdoc />
    /// <param name="reader">JSON 读取器</param>
    /// <param name="typeToConvert">待转换类型</param>
    /// <param name="options">序列化选项</param>
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => (reader.TokenType switch
    {
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
        JsonTokenType.Number => reader.GetDouble(),
        JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
        JsonTokenType.String => reader.GetString(),
        _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
    })!;

    /// <inheritdoc />
    /// <param name="writer">JSON 写入器</param>
    /// <param name="objectToWrite">待写入对象</param>
    /// <param name="options">序列化选项</param>
    public override void Write(Utf8JsonWriter writer, object objectToWrite, JsonSerializerOptions options)
    {
        Type runtimeType = objectToWrite.GetType();
        if (runtimeType == typeof(object))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
            return;
        }

        JsonSerializer.Serialize(writer, objectToWrite, runtimeType, options);
    }
}
