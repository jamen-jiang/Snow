using System;
using System.Globalization;

namespace Snow.Reflection;

/// <summary>
/// 类型转换与判定辅助方法(移植自 ABP, 只保留 ChangeType 相关部分)。
/// </summary>
public static class TypeHelper
{
    /// <summary>
    /// Converts the value like <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> with the invariant culture.
    /// A float or double converted to decimal is rounded to the 7 or 15 significant digits the type holds, which is what
    /// earlier .NET versions returned for amounts (0.1d is 0.1m).
    /// </summary>
    public static object ChangeType(object value, Type conversionType)
    {
#if NET11_0_OR_GREATER
        // Since .NET 11 the conversion keeps the binary error: 0.1d became 0.1000000000000000055511151231m, so an amount
        // read back from JSON (where it is a double) would no longer equal the decimal that was saved.
        if (conversionType == typeof(decimal))
        {
            switch (value)
            {
                case double doubleValue:
                    return ToDecimal(doubleValue, doubleValue.ToString("G15", CultureInfo.InvariantCulture));
                case float floatValue:
                    return ToDecimal(floatValue, floatValue.ToString("G7", CultureInfo.InvariantCulture));
            }
        }
#endif

        return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
    }

#if NET11_0_OR_GREATER
    private static decimal ToDecimal(double value, string roundedValue)
    {
        // Convert.ChangeType throws OverflowException for NaN, infinity and values outside the decimal range; parsing the
        // rounded text would instead throw FormatException or round a value just past the range back into it.
        if (!(Math.Abs(value) < 7.922816251426434E+28))
        {
            throw new OverflowException("Value was either too large or too small for a Decimal.");
        }

        return decimal.Parse(roundedValue, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
#endif

    public static bool IsNullable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static bool IsAsyncEnumerable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>);
    }

    public static bool IsNullableEnum(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
               type.GenericTypeArguments.Length == 1 &&
               type.GenericTypeArguments[0].IsEnum;
    }

    public static Type GetFirstGenericArgumentIfNullable(this Type t)
    {
        if (t.GetGenericArguments().Length > 0 && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return t.GetGenericArguments().First();
        }

        return t;
    }
}
