using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Auditing;

/// <summary>
/// 基于 System.Text.Json 的审计序列化器
/// <para>循环引用用忽略策略处理, 最大深度 32; 序列化失败与超长都退化成文本, 绝不因为参数怪就丢日志。</para>
/// </summary>
[SingletonService]
public sealed class JsonAuditSerializer : IAuditSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 32,
    };

    private readonly AuditingOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">审计配置</param>
    public JsonAuditSerializer(IOptions<AuditingOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public string Serialize(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        string text;
        try
        {
            text = JsonSerializer.Serialize(value, value.GetType(), SerializerOptions);
        }
        catch (Exception exception)
        {
            text = "<序列化失败: " + exception.GetType().Name + ": " + exception.Message + ">";
        }

        int maxLength = _options.MaxSerializedLength;
        if (maxLength > 0 && text.Length > maxLength)
        {
            text = text[..maxLength];
        }

        return text;
    }
}
