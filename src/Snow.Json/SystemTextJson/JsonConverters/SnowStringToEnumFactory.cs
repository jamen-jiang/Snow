using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snow.Json.SystemTextJson.JsonConverters;

/// <summary>
/// 枚举字符串转换器工厂(对应 ABP 的 <c>AbpStringToEnumFactory</c>)。
/// </summary>
public class SnowStringToEnumFactory : JsonConverterFactory
{
    private readonly JsonNamingPolicy? _namingPolicy;
    private readonly bool _allowIntegerValues;

    /// <summary>
    /// 使用默认命名策略并允许整数值。
    /// </summary>
    public SnowStringToEnumFactory()
        : this(namingPolicy: null, allowIntegerValues: true)
    {
    }

    /// <summary>
    /// 构造器。
    /// </summary>
    /// <param name="namingPolicy">枚举名称命名策略, 可为 null</param>
    /// <param name="allowIntegerValues">是否允许用整数表示枚举</param>
    public SnowStringToEnumFactory(JsonNamingPolicy? namingPolicy, bool allowIntegerValues)
    {
        _namingPolicy = namingPolicy;
        _allowIntegerValues = allowIntegerValues;
    }

    /// <inheritdoc />
    /// <param name="typeToConvert">待转换类型</param>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    /// <param name="typeToConvert">待转换类型</param>
    /// <param name="options">序列化选项</param>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(
            typeof(SnowStringToEnumConverter<>).MakeGenericType(typeToConvert),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            new object?[] { _namingPolicy, _allowIntegerValues },
            culture: null)!;
    }
}
