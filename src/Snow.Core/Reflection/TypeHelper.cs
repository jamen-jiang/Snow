using System;
using System.Globalization;

namespace Snow.Reflection;

/// <summary>
/// 类型转换与判定辅助方法(移植自 ABP, 只保留 ChangeType 相关部分)。
/// </summary>
public static class TypeHelper
{
    /// <summary>
    /// 用不变文化做类型转换, 行为等同 <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/>。
    /// 从 float 或 double 转 decimal 时按类型能表示的 7 位或 15 位有效数字四舍五入, 这也是早期 .NET 版本
    /// 对金额给出的结果(0.1d 转出来就是 0.1m)。
    /// </summary>
    /// <param name="value">要转换的值</param>
    /// <param name="conversionType">目标类型</param>
    /// <returns>转换后的值</returns>
    public static object ChangeType(object value, Type conversionType)
    {
#if NET11_0_OR_GREATER
        // .NET 11 起这个转换会把二进制误差一起带过来: 0.1d 会变成 0.1000000000000000055511151231m,
        // 于是从 JSON 里读回来的金额(JSON 里是 double)就不再等于当初存进去的 decimal 了。
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
    /// <summary>
    /// 把按有效数字四舍五入后的文本解析成 decimal, 并补上 decimal 的范围检查。
    /// </summary>
    /// <param name="value">原始浮点值, 只用于范围判断</param>
    /// <param name="roundedValue">已经按有效数字转好的十进制文本</param>
    /// <returns>解析出的 decimal</returns>
    private static decimal ToDecimal(double value, string roundedValue)
    {
        // Convert.ChangeType 遇到 NaN、无穷大以及超出 decimal 范围的值会抛 OverflowException;
        // 而直接解析四舍五入后的文本则可能抛 FormatException, 或者把刚刚越界的值又舍回范围内。
        if (!(Math.Abs(value) < 7.922816251426434E+28))
        {
            throw new OverflowException("Value was either too large or too small for a Decimal.");
        }

        return decimal.Parse(roundedValue, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
#endif

    /// <summary>
    /// 判断类型是否为可空值类型 <see cref="Nullable{T}"/>。
    /// </summary>
    /// <param name="type">要判断的类型</param>
    public static bool IsNullable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    /// <summary>
    /// 判断类型是否为 <see cref="IAsyncEnumerable{T}"/>。
    /// </summary>
    /// <param name="type">要判断的类型</param>
    public static bool IsAsyncEnumerable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>);
    }

    /// <summary>
    /// 判断类型是否为可空的枚举类型。
    /// </summary>
    /// <param name="type">要判断的类型</param>
    public static bool IsNullableEnum(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
               type.GenericTypeArguments.Length == 1 &&
               type.GenericTypeArguments[0].IsEnum;
    }

    /// <summary>
    /// 如果类型是可空值类型, 就取出它的第一个泛型参数(即被包裹的那个类型), 否则原样返回。
    /// </summary>
    /// <param name="t">要处理的类型</param>
    public static Type GetFirstGenericArgumentIfNullable(this Type t)
    {
        if (t.GetGenericArguments().Length > 0 && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return t.GetGenericArguments().First();
        }

        return t;
    }
}
