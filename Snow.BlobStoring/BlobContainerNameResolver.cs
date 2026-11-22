using System.Reflection;

namespace Snow.BlobStoring;

/// <summary>
/// 容器名解析: 优先 BlobContainerName 特性, 否则类型名去掉 BlobContainer 后缀
/// </summary>
public static class BlobContainerNameResolver
{
    private const string Suffix = "BlobContainer";

    /// <summary>
    /// 解析容器名
    /// </summary>
    /// <param name="containerType">容器标记类型</param>
    /// <returns>容器名</returns>
    public static string Resolve(Type containerType)
    {
        ArgumentNullException.ThrowIfNull(containerType);

        BlobContainerNameAttribute? attribute = containerType.GetCustomAttribute<BlobContainerNameAttribute>(inherit: true);
        if (!string.IsNullOrWhiteSpace(attribute?.Name))
        {
            return attribute!.Name;
        }

        string name = containerType.Name;
        int arityIndex = name.IndexOf((char)96);
        if (arityIndex >= 0)
        {
            name = name[..arityIndex];
        }

        if (name.Length > Suffix.Length && name.EndsWith(Suffix, StringComparison.Ordinal))
        {
            name = name[..^Suffix.Length];
        }

        return name;
    }
}
