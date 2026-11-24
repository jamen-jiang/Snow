using System;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;

namespace Snow.Json.SystemTextJson;

/// <summary>
/// 默认 JSON 类型信息解析器(对应 ABP 的 <c>AbpDefaultJsonTypeInfoResolver</c>)，会附加配置好的修饰器。
/// </summary>
public class SnowDefaultJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    /// <summary>
    /// 构造器: 从选项里取出修饰器并加入解析器。
    /// </summary>
    /// <param name="options">JSON 类型信息修饰器选项</param>
    public SnowDefaultJsonTypeInfoResolver(IOptions<SnowSystemTextJsonSerializerModifiersOptions> options)
    {
        foreach (Action<JsonTypeInfo> modifier in options.Value.Modifiers)
        {
            Modifiers.Add(modifier);
        }
    }
}
