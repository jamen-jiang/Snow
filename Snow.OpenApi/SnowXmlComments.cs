using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.OpenApi;

namespace Snow.OpenApi;

/// <summary>
/// 从程序集旁的 XML 注释文件里取出摘要, 再贴到文档上
/// <para>官方 OpenAPI 生成器不读 XML 注释, 这一层把 <c>GenerateDocumentationFile</c> 的产出补回去。</para>
/// </summary>
internal sealed class SnowXmlComments
{
    private readonly Dictionary<string, XmlCommentMember> _members = new(StringComparer.Ordinal);

    /// <summary>
    /// 读取一组 XML 注释文件
    /// </summary>
    /// <param name="paths">文件路径</param>
    /// <returns>注释集合</returns>
    public static SnowXmlComments Load(IEnumerable<string> paths)
    {
        SnowXmlComments comments = new();

        foreach (string path in paths)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(path);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (XElement member in document.Descendants("member"))
            {
                string? name = member.Attribute("name")?.Value;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                XmlCommentMember parsed = new()
                {
                    Summary = ReadText(member.Element("summary")),
                    Remarks = ReadText(member.Element("remarks")),
                    Returns = ReadText(member.Element("returns")),
                };

                foreach (XElement parameter in member.Elements("param"))
                {
                    string? parameterName = parameter.Attribute("name")?.Value;
                    string? text = ReadText(parameter);
                    if (!string.IsNullOrWhiteSpace(parameterName) && !string.IsNullOrWhiteSpace(text))
                    {
                        parsed.Parameters[parameterName] = text;
                    }
                }

                comments._members[Shorten(name)] = parsed;
            }
        }

        return comments;
    }

    /// <summary>
    /// 把摘要写进操作
    /// </summary>
    /// <param name="operation">OpenAPI 操作</param>
    /// <param name="method">对应的处理方法</param>
    public void ApplyToOperation(OpenApiOperation operation, MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(method);

        if (!_members.TryGetValue(MethodKey(method), out XmlCommentMember? member))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(member.Summary))
        {
            operation.Summary = member.Summary;
        }

        if (!string.IsNullOrWhiteSpace(member.Remarks))
        {
            operation.Description = member.Remarks;
        }

        if (operation.Parameters is null)
        {
            return;
        }

        foreach (IOpenApiParameter parameter in operation.Parameters)
        {
            if (parameter.Name is not null &&
                member.Parameters.TryGetValue(parameter.Name, out string? description) &&
                parameter is OpenApiParameter concrete &&
                string.IsNullOrWhiteSpace(concrete.Description))
            {
                concrete.Description = description;
            }
        }

        if (!string.IsNullOrWhiteSpace(member.Returns) &&
            operation.Responses is not null &&
            operation.Responses.TryGetValue("200", out IOpenApiResponse? response) &&
            response is OpenApiResponse concreteResponse &&
            string.IsNullOrWhiteSpace(concreteResponse.Description))
        {
            concreteResponse.Description = member.Returns;
        }
    }

    /// <summary>
    /// 把属性的摘要写进对象结构
    /// </summary>
    /// <param name="schema">对象结构</param>
    /// <param name="type">对应的 CLR 类型</param>
    public void ApplyToSchema(IOpenApiSchema schema, Type type)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(type);

        if (schema.Properties is null)
        {
            return;
        }

        string typeName = type.FullName ?? type.Name;

        foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties)
        {
            if (!_members.TryGetValue("P:" + typeName + "." + property.Key, out XmlCommentMember? member) ||
                string.IsNullOrWhiteSpace(member.Summary) ||
                property.Value is not OpenApiSchema concrete ||
                !string.IsNullOrWhiteSpace(concrete.Description))
            {
                continue;
            }

            concrete.Description = member.Summary;
        }
    }

    private static string MethodKey(MethodInfo method)
    {
        string typeName = method.DeclaringType?.FullName ?? string.Empty;

        return "M:" + typeName + "." + method.Name;
    }

    private static string Shorten(string memberName)
    {
        int index = memberName.IndexOf('(');

        return index >= 0 ? memberName[..index] : memberName;
    }

    private static string? ReadText(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        StringBuilder builder = new();

        foreach (XNode node in element.DescendantNodes())
        {
            if (node is XText text)
            {
                builder.Append(text.Value);
            }
            else if (node is XElement nested && nested.Name == "see" && nested.Attribute("cref") is { } cref)
            {
                string reference = cref.Value;
                int separator = reference.IndexOf(':');
                builder.Append(separator >= 0 ? reference[(separator + 1)..] : reference);
            }
        }

        string value = string.Join(' ', builder.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

/// <summary>
/// XML 注释里一条成员记录
/// </summary>
internal sealed class XmlCommentMember
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// 返回值说明
    /// </summary>
    public string? Returns { get; set; }

    /// <summary>
    /// 参数说明
    /// </summary>
    public Dictionary<string, string> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);
}
