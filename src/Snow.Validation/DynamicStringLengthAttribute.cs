using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using Snow;

namespace Snow.Validation;

/// <summary>
/// 运行时从 <c>sourceType</c> 的公共静态属性读取
/// <see cref="StringLengthAttribute.MaximumLength"/> 与 <see cref="StringLengthAttribute.MinimumLength"/> 的动态字符串长度特性。
/// </summary>
public class DynamicStringLengthAttribute : StringLengthAttribute
{
    private static readonly FieldInfo? MaximumLengthField;

    static DynamicStringLengthAttribute()
    {
        MaximumLengthField = typeof(StringLengthAttribute).GetField(
            "<MaximumLength>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Debug.Assert(MaximumLengthField != null, nameof(MaximumLengthField) + " != null");
    }

    /// <param name="sourceType">提供静态属性值的类型</param>
    /// <param name="maximumLengthPropertyName">最大长度的公共静态属性名</param>
    /// <param name="minimumLengthPropertyName">最小长度的公共静态属性名</param>
    public DynamicStringLengthAttribute(
        Type sourceType,
        string? maximumLengthPropertyName,
        string? minimumLengthPropertyName = null)
        : base(0)
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

        if (minimumLengthPropertyName != null)
        {
            var minimumLengthProperty = sourceType.GetProperty(
                minimumLengthPropertyName,
                BindingFlags.Static | BindingFlags.Public
            );
            Debug.Assert(minimumLengthProperty != null, nameof(minimumLengthProperty) + " != null");
            MinimumLength = (int)minimumLengthProperty?.GetValue(null)!;
        }
    }
}
