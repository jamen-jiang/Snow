using System.Text.Json;

namespace Snow.BackgroundJobs;

/// <summary>
/// 任务参数编解码: 入队写库与执行读库用同一份配置, 避免两边策略不一致
/// </summary>
public static class BackgroundJobSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 序列化任务参数
    /// </summary>
    /// <param name="value">参数对象</param>
    /// <param name="type">参数类型</param>
    /// <returns>JSON 文本</returns>
    public static string Serialize(object? value, Type type)
    {
        return JsonSerializer.Serialize(value, type, SerializerOptions);
    }

    /// <summary>
    /// 反序列化任务参数
    /// </summary>
    /// <param name="value">JSON 文本</param>
    /// <param name="type">参数类型</param>
    /// <returns>参数对象</returns>
    public static object? Deserialize(string value, Type type)
    {
        return JsonSerializer.Deserialize(value, type, SerializerOptions);
    }
}
