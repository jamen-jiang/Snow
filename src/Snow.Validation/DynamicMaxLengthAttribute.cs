using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using Snow;

namespace Snow.Validation;

/// <summary>
/// 运行时从 <c>sourceType</c> 的公共静态属性读取 <see cref="MaxLengthAttribute.Length"/> 的动态最大长度特性。
/// </summary>
public class DynamicMaxLengthAttribute : MaxLengthAttribute
{
    private static readonly FieldInfo? MaximumLengthField;

    static DynamicMaxLengthAttribute()
    {
        MaximumLengthField = typeof(MaxLengthAttribute).GetField(
            "<Length>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Debug.Assert(MaximumLengthField != null, nameof(MaximumLengthField) + " != null");
    }

    /// <param name="sourceType">提供静态属性值的类型</param>
    /// <param name="maximumLengthPropertyName">最大长度的公共静态属性名</param>
    public DynamicMaxLengthAttribute(
        Type sourceType,
        string? maximumLengthPropertyName)
    {
        Check.NotNull(sourceType, nameof(sourceType));

        if (maximumLengthPropertyName != null)
        {
            var maximumLengthProperty = sourceType.GetProperty(
                maximumLengthPropertyName,
                BindingFlags.Static | BindingFlags.Public
            );
            Debug.Assert(maximumLengthProperty != null, nameof(maximumLengthProperty) + " != null");
            MaximumLengthField?.SetValue(this, (int)maximumLengthProperty?.GetValue(null)!);
        }
    }
}
