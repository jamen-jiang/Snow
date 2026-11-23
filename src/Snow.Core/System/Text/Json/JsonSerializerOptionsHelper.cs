using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace System.Text.Json;

/// <summary>
/// 以现成的 <see cref="JsonSerializerOptions"/> 为模板派生新选项: 去掉指定转换器, 再补一批转换器。
/// </summary>
public static class JsonSerializerOptionsHelper
{
    /// <summary>
    /// 复制一份选项, 去掉指定的那个转换器, 再补上给定转换器。
    /// </summary>
    /// <param name="baseOptions">作为模板的选项</param>
    /// <param name="removeConverter">要移除的转换器</param>
    /// <param name="addConverters">要追加的转换器</param>
    /// <returns>派生出的新选项</returns>
    public static JsonSerializerOptions Create(JsonSerializerOptions baseOptions, JsonConverter removeConverter, params JsonConverter[] addConverters)
    {
        return Create(baseOptions, x => x == removeConverter, addConverters);
    }

    /// <summary>
    /// 复制一份选项, 按条件批量移除转换器, 再补上给定转换器。
    /// </summary>
    /// <param name="baseOptions">作为模板的选项</param>
    /// <param name="removeConverterPredicate">判断"移除哪些转换器"的条件</param>
    /// <param name="addConverters">要追加的转换器</param>
    /// <returns>派生出的新选项</returns>
    public static JsonSerializerOptions Create(JsonSerializerOptions baseOptions, Func<JsonConverter, bool> removeConverterPredicate, params JsonConverter[] addConverters)
    {
        var options = new JsonSerializerOptions(baseOptions);
        options.Converters.RemoveAll(removeConverterPredicate);
        options.Converters.AddIfNotContains(addConverters);
        return options;
    }
}
