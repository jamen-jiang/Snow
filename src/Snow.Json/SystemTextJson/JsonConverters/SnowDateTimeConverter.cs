using System;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Timing;

namespace Snow.Json.SystemTextJson.JsonConverters;

/// <summary>
/// 日期时间转换器(对应 ABP 的 <c>AbpDateTimeConverter</c>)。
/// </summary>
[TransientService]
public class SnowDateTimeConverter : SnowDateTimeConverterBase<DateTime>
{
    /// <summary>
    /// 构造器。
    /// </summary>
    /// <param name="clock">时钟</param>
    /// <param name="snowJsonOptions">JSON 选项</param>
    /// <param name="currentTimezoneProvider">当前时区提供器</param>
    /// <param name="timezoneProvider">时区提供器</param>
    public SnowDateTimeConverter(
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
    public virtual SnowDateTimeConverter SkipDateTimeNormalization()
    {
        IsSkipDateTimeNormalization = true;
        return this;
    }

    /// <inheritdoc />
    /// <param name="reader">JSON 读取器</param>
    /// <param name="typeToConvert">待转换类型</param>
    /// <param name="options">序列化选项</param>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (Options.InputDateTimeFormats.Any() && reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Reader's TokenType is not String!");
        }

        return TryReadDateTime(ref reader, out DateTime result)
            ? result
            : throw new JsonException("Can't get datetime from the reader!");
    }

    /// <inheritdoc />
    /// <param name="writer">JSON 写入器</param>
    /// <param name="value">日期时间</param>
    /// <param name="options">序列化选项</param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        WriteDateTime(writer, value);
    }
}
