using System;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using Snow.Json.SystemTextJson.JsonConverters;
using Snow.Reflection;
using Snow.Timing;

namespace Snow.Json.SystemTextJson.Modifiers;

/// <summary>
/// 日期时间转换器修饰器(对应 ABP 的 <c>AbpDateTimeConverterModifier</c>)。
/// </summary>
public class SnowDateTimeConverterModifier
{
    private readonly SnowDateTimeConverter _snowDateTimeConverter;
    private readonly SnowNullableDateTimeConverter _snowNullableDateTimeConverter;

    /// <summary>
    /// 构造器。
    /// </summary>
    /// <param name="snowDateTimeConverter">日期时间转换器</param>
    /// <param name="snowNullableDateTimeConverter">可空日期时间转换器</param>
    public SnowDateTimeConverterModifier(SnowDateTimeConverter snowDateTimeConverter, SnowNullableDateTimeConverter snowNullableDateTimeConverter)
    {
        _snowDateTimeConverter = snowDateTimeConverter;
        _snowNullableDateTimeConverter = snowNullableDateTimeConverter;
    }

    /// <summary>
    /// 创建类型信息修改动作。
    /// </summary>
    /// <returns>类型信息修改动作</returns>
    public Action<JsonTypeInfo> CreateModifyAction()
    {
        return Modify;
    }

    private void Modify(JsonTypeInfo jsonTypeInfo)
    {
        if (ReflectionHelper.GetAttributesOfMemberOrDeclaringType<DisableDateTimeNormalizationAttribute>(jsonTypeInfo.Type).Any())
        {
            return;
        }

        foreach (JsonPropertyInfo property in jsonTypeInfo.Properties.Where(x => x.PropertyType == typeof(DateTime) || x.PropertyType == typeof(DateTime?)))
        {
            if (property.AttributeProvider == null ||
                !property.AttributeProvider.GetCustomAttributes(typeof(DisableDateTimeNormalizationAttribute), false).Any())
            {
                property.CustomConverter = property.PropertyType == typeof(DateTime)
                    ? _snowDateTimeConverter
                    : _snowNullableDateTimeConverter;
            }
        }
    }
}
