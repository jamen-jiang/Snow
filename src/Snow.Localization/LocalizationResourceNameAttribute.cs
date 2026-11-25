using System;
using System.Linq;

namespace Snow.Localization;

/// <summary>
/// 标注类型所属的本地化资源名(对应 ABP 的 <c>LocalizationResourceNameAttribute</c>)
/// </summary>
public class LocalizationResourceNameAttribute : Attribute
{
    /// <summary>
    /// 资源名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">资源名</param>
    public LocalizationResourceNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 取类型上的 <see cref="LocalizationResourceNameAttribute"/>; 没有则返回 <c>null</c>。
    /// </summary>
    public static LocalizationResourceNameAttribute? GetOrNull(Type resourceType)
        => resourceType.GetCustomAttributes(inherit: true).OfType<LocalizationResourceNameAttribute>().FirstOrDefault();

    /// <summary>
    /// 取类型所属本地化资源名; 未标注时退回类型全名。
    /// </summary>
    public static string GetName(Type resourceType)
        => GetOrNull(resourceType)?.Name ?? resourceType.FullName!;
}
