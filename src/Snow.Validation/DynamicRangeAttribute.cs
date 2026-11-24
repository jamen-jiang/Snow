using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using Snow;

namespace Snow.Validation;

/// <summary>
/// 运行时从 <c>sourceType</c> 的公共静态属性读取 <see cref="RangeAttribute.Minimum"/> 与
/// <see cref="RangeAttribute.Maximum"/> 的动态范围特性。
/// </summary>
public class DynamicRangeAttribute : RangeAttribute
{
    private static readonly FieldInfo? MaximumField;
    private static readonly FieldInfo? MinimumField;

    static DynamicRangeAttribute()
    {
        MaximumField = typeof(RangeAttribute).GetField(
            "<Maximum>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Debug.Assert(MaximumField != null, nameof(MaximumField) + " != null");

        MinimumField = typeof(RangeAttribute).GetField(
            "<Minimum>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Debug.Assert(MinimumField != null, nameof(MinimumField) + " != null");
    }

    /// <param name="sourceType">提供静态属性值的类型</param>
    /// <param name="operandType">范围参数的类型, 必须实现 IComparable</param>
    /// <param name="minimumPropertyName">最小值的公共静态属性名</param>
    /// <param name="maximumPropertyName">最大值的公共静态属性名</param>
    public DynamicRangeAttribute(
        Type sourceType,
        Type operandType,
        string? minimumPropertyName,
        string? maximumPropertyName
    )
        : base(operandType, string.Empty, string.Empty)
    {
        Check.NotNull(sourceType, nameof(sourceType));

        if (minimumPropertyName != null)
        {
            var minimumProperty = sourceType.GetProperty(
                minimumPropertyName,
                BindingFlags.Static | BindingFlags.Public
            );
            Debug.Assert(minimumProperty != null, nameof(minimumProperty) + " != null");
            MinimumField?.SetValue(this, minimumProperty?.GetValue(null));
        }

        if (maximumPropertyName != null)
        {
            var maximumProperty = sourceType.GetProperty(
                maximumPropertyName,
                BindingFlags.Static | BindingFlags.Public
            );
            Debug.Assert(maximumProperty != null, nameof(maximumProperty) + " != null");
            MaximumField?.SetValue(this, maximumProperty?.GetValue(null));
        }
    }
}
