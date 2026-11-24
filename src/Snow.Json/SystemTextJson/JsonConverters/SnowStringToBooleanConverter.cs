using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snow.Json.SystemTextJson.JsonConverters;

/// <summary>
/// 字符串转布尔转换器(对应 ABP 的 <c>AbpStringToBooleanConverter</c>)。
/// </summary>
public class SnowStringToBooleanConverter : JsonConverter<bool>
{
    /// <inheritdoc />
    /// <param name="reader">JSON 读取器</param>
    /// <param name="typeToConvert">待转换类型</param>
    /// <param name="options">序列化选项</param>
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            if (Utf8Parser.TryParse(span, out bool b1, out int bytesConsumed) && span.Length == bytesConsumed)
            {
                return b1;
            }

            if (bool.TryParse(reader.GetString(), out bool b2))
            {
                return b2;
            }
        }

        return reader.GetBoolean();
    }

    /// <inheritdoc />
    /// <param name="writer">JSON 写入器</param>
    /// <param name="value">布尔值</param>
    /// <param name="options">序列化选项</param>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
