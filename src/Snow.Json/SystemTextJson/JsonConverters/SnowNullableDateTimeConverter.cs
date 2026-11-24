using System;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Timing;

namespace Snow.Json.SystemTextJson.JsonConverters;

/// <summary>
/// 可空日期时间转换器(对应 ABP 的 <c>AbpNullableDateTimeConverter</c>)。
/// </summary>
[TransientService]
public class SnowNullableDateTimeConverter : SnowDateTimeConverterBase<DateTime?>
{
    /// <summary>
    /// 构造器。
    /// </summary>
    /// <param name="clock">时钟</param>
    /// <param name="snowJsonOptions">JSON 选项</param>
    /// <param name="currentTimezoneProvider">当前时区提供器</param>
    /// <param name="timezoneProvider">时区提供器</param>
    public SnowNullableDateTimeConverter(
        IClock clock,
        IOptions<SnowJsonOptions> snowJsonOptions,
        ICurrentTimezoneProvider currentTimezoneProvider,
        ITimezoneProvider timezoneProvider)
        : base(clock, snowJsonOptions, currentTimezoneProvider, timezoneProvider)
    {
    }

    /// <summary>
    /// 跳过日期时间规范化。
    /// </summary>
    /// <returns>当前转换器</returns>
    public virtual SnowNullableDateTimeConverter SkipDateTimeNormalization()
    {
        IsSkipDateTimeNormalization = true;
        return this;
    }

    /// <inheritdoc />
    /// <param name="reader">JSON 读取器</param>
    /// <param name="typeToConvert">待转换类型</param>
    /// <param name="options">序列化选项</param>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (Options.InputDateTimeFormats.Any() && reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Reader's TokenType is not String!");
        }

        if (TryReadDateTime(ref reader, out DateTime result))
        {
            return result;
        }

        return null;
    }

    /// <inheritdoc />
    /// <param name="writer">JSON 写入器</param>
    /// <param name="value">可空日期时间</param>
    /// <param name="options">序列化选项</param>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        WriteDateTime(writer, value.Value);
    }
}
