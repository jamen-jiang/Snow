namespace Snow.Json;

/// <summary>
/// <see cref="ISnowJsonSerializer"/> 扩展: 与 ABP 的 <c>IJsonSerializer.Serialize(value, indented)</c> 同形的调用口。
/// </summary>
public static class SnowJsonSerializerExtensions
{
    /// <summary>
    /// 序列化成字符串(带缩进口)
    /// </summary>
    /// <param name="serializer">序列化器</param>
    /// <param name="value">待序列化的值</param>
    /// <param name="indented">是否缩进(当前实现以底层选项为准, 此处保持签名一致)</param>
    /// <returns>JSON 字符串</returns>
    public static string Serialize(this ISnowJsonSerializer serializer, object? value, bool indented)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        _ = indented;
        return serializer.Serialize(value);
    }
}
