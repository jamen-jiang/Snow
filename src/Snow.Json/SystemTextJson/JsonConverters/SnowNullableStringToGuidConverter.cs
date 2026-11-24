using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snow.Json.SystemTextJson.JsonConverters;

/// <summary>
/// 字符串转可空 Guid 转换器(对应 ABP 的 <c>AbpNullableStringToGuidConverter</c>)。
/// </summary>
public class SnowNullableStringToGuidConverter : JsonConverter<Guid?>
{
    /// <inheritdoc />
    /// <param name="reader">JSON 读取器</param>
    /// <param name="typeToConvert">待转换类型</param>
    /// <param name="options">序列化选项</param>
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? guidString = reader.GetString();
            string[] formats = { "N", "D", "B", "P", "X" };
            foreach (string format in formats)
            {
                if (Guid.TryParseExact(guidString, format, out Guid guid))
                {
                    return guid;
                }
            }
        }

        if (reader.TryGetGuid(out Guid guid2))
        {
            return guid2;
        }

        return null;
    }

    /// <inheritdoc />
    /// <param name="writer">JSON 写入器</param>
    /// <param name="value">可空 Guid 值</param>
    /// <param name="options">序列化选项</param>
    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
