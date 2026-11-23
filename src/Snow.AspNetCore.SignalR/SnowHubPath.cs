namespace Snow.AspNetCore.SignalR;

/// <summary>
/// Hub 端点路径推导
/// </summary>
public static class SnowHubPath
{
    /// <summary>
    /// 按 "标注路径 → /signalr/{类名去 Hub 后连字符化}" 推导路径
    /// </summary>
    /// <param name="hubType">Hub 类型</param>
    /// <returns>端点路径</returns>
    public static string For(Type hubType)
    {
        ArgumentNullException.ThrowIfNull(hubType);

        SnowHubAttribute? attribute = hubType
            .GetCustomAttributes(typeof(SnowHubAttribute), inherit: true)
            .OfType<SnowHubAttribute>()
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(attribute?.Path))
        {
            return attribute!.Path!;
        }

        string name = hubType.Name;
        if (name.EndsWith("Hub", StringComparison.Ordinal) && name.Length > 3)
        {
            name = name[..^3];
        }

        return "/signalr/" + ToKebabCase(name);
    }

    /// <summary>
    /// 把 PascalCase 转成 kebab-case
    /// </summary>
    /// <param name="value">原字符串</param>
    /// <returns>连字符形式</returns>
    public static string ToKebabCase(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        System.Text.StringBuilder builder = new(value.Length + 8);
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (char.IsUpper(current))
            {
                bool hasPrevious = index > 0;
                bool previousIsLowerOrDigit = hasPrevious && (char.IsLower(value[index - 1]) || char.IsDigit(value[index - 1]));
                if (hasPrevious && previousIsLowerOrDigit)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}
