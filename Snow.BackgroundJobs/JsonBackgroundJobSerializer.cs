using System.Text.Json;

namespace Snow.BackgroundJobs;

/// <summary>
/// 基于 System.Text.Json 的任务参数序列化(Web 命名策略, 与 HTTP 层一致)
/// </summary>
public sealed class JsonBackgroundJobSerializer : IBackgroundJobSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public string Serialize(object? value, Type type)
    {
        return JsonSerializer.Serialize(value, type, SerializerOptions);
    }

    /// <inheritdoc />
    public object? Deserialize(string value, Type type)
    {
        return JsonSerializer.Deserialize(value, type, SerializerOptions);
    }
}
