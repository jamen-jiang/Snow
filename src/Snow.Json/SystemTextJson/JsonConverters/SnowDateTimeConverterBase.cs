using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Snow.Json;
using Snow.Timing;

namespace Snow.Json.SystemTextJson.JsonConverters;

/// <summary>
/// 日期时间转换器基类(对应 ABP 的 <c>AbpDateTimeConverterBase</c>)。
/// </summary>
/// <typeparam name="T">日期时间类型</typeparam>
public abstract class SnowDateTimeConverterBase<T> : JsonConverter<T>
{
    /// <summary>
    /// 日志记录器。
    /// </summary>
    public ILogger<SnowDateTimeConverterBase<T>> Logger { get; set; }

    /// <summary>
    /// 时钟。
    /// </summary>
    protected IClock Clock { get; }

    /// <summary>
    /// JSON 选项。
    /// </summary>
    protected SnowJsonOptions Options { get; }

    /// <summary>
    /// 当前时区提供器。
    /// </summary>
    protected ICurrentTimezoneProvider CurrentTimezoneProvider { get; }

    /// <summary>
    /// 时区提供器。
    /// </summary>
    protected ITimezoneProvider TimezoneProvider { get; }

    /// <summary>
    /// 是否跳过日期时间的规范化。
    /// </summary>
    protected bool IsSkipDateTimeNormalization { get; set; }

    /// <summary>
    /// 构造器。
    /// </summary>
    /// <param name="clock">时钟</param>
    /// <param name="snowJsonOptions">JSON 选项</param>
    /// <param name="currentTimezoneProvider">当前时区提供器</param>
    /// <param name="timezoneProvider">时区提供器</param>
    protected SnowDateTimeConverterBase(
        IClock clock,
        IOptions<SnowJsonOptions> snowJsonOptions,
        ICurrentTimezoneProvider currentTimezoneProvider,
        ITimezoneProvider timezoneProvider)
    {
        Logger = NullLogger<SnowDateTimeConverterBase<T>>.Instance;

        Clock = clock;
        CurrentTimezoneProvider = currentTimezoneProvider;
        TimezoneProvider = timezoneProvider;
        Options = snowJsonOptions.Value;
    }

    /// <summary>
    /// 从读取器解析日期时间。
    /// </summary>
    /// <param name="reader">JSON 读取器</param>
    /// <param name="value">解析结果</param>
    /// <returns>是否解析成功</returns>
    protected bool TryReadDateTime(ref Utf8JsonReader reader, out DateTime value)
    {
        value = default;

        if (Options.InputDateTimeFormats.Any())
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                return false;
            }

            string? s = reader.GetString();
            foreach (string format in Options.InputDateTimeFormats)
            {
                if (!DateTime.TryParseExact(s, format, CultureInfo.CurrentUICulture, DateTimeStyles.None, out DateTime d1))
                {
                    continue;
                }

                value = Normalize(d1);
                return true;
            }
        }

        if (reader.TryGetDateTime(out DateTime d2))
        {
            value = Normalize(d2);
            return true;
        }

        string? dateText = reader.GetString();
        if (dateText.IsNullOrWhiteSpace())
        {
            return false;
        }

        if (!DateTime.TryParse(dateText, CultureInfo.CurrentUICulture, DateTimeStyles.None, out DateTime d3))
        {
            return false;
        }

        value = Normalize(d3);
        return true;
    }

    /// <summary>
    /// 把日期时间写入写入器。
    /// </summary>
    /// <param name="writer">JSON 写入器</param>
    /// <param name="value">日期时间</param>
    protected void WriteDateTime(Utf8JsonWriter writer, DateTime value)
    {
        if (Options.OutputDateTimeFormat.IsNullOrWhiteSpace())
        {
            writer.WriteStringValue(Normalize(value));
        }
        else
        {
            writer.WriteStringValue(Normalize(value).ToString(Options.OutputDateTimeFormat, CultureInfo.CurrentUICulture));
        }
    }

    /// <summary>
    /// 规范化日期时间。
    /// </summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>规范化后的日期时间</returns>
    protected virtual DateTime Normalize(DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Unspecified ||
            !Clock.SupportsMultipleTimezone ||
            CurrentTimezoneProvider.TimeZone.IsNullOrWhiteSpace())
        {
            return IsSkipDateTimeNormalization ? dateTime : Clock.Normalize(dateTime);
        }

        string timeZone = CurrentTimezoneProvider.TimeZone;
        try
        {
            dateTime = TimezoneProvider.ConvertUnspecifiedToUtc(dateTime, timeZone);
        }
        catch
        {
            Logger.LogWarning("Could not convert DateTime with unspecified Kind using timezone '{TimeZone}'.", timeZone);
        }

        return IsSkipDateTimeNormalization ? dateTime : Clock.Normalize(dateTime);
    }
}
