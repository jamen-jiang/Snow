using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace Snow.Json.SystemTextJson;

/// <summary>
/// JSON 类型信息修饰器选项(对应 ABP 的 <c>AbpSystemTextJsonSerializerModifiersOptions</c>)。
/// </summary>
public class SnowSystemTextJsonSerializerModifiersOptions
{
    /// <summary>
    /// 类型信息修饰器列表。
    /// </summary>
    public List<Action<JsonTypeInfo>> Modifiers { get; }

    /// <summary>
    /// 构造器: 初始化空的修饰器列表。
    /// </summary>
    public SnowSystemTextJsonSerializerModifiersOptions()
    {
        Modifiers = new List<Action<JsonTypeInfo>>();
    }
}
