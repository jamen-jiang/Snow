using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Snow;

/// <summary>
/// 参数与值的校验辅助方法: 校验不通过时直接抛 <see cref="ArgumentException"/> 一类异常, 校验通过则原样返回值。
/// </summary>
[DebuggerStepThrough]
public static class Check
{
    /// <summary>
    /// 参数为 null 时抛 <see cref="ArgumentNullException"/>, 否则原样返回, 便于链式取值。
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的原值</returns>
    public static T NotNull<T>(
        [System.Diagnostics.CodeAnalysis.NotNull] T? value,
        string parameterName
    )
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return value;
    }

    /// <summary>
    /// 参数为 null 时抛 <see cref="ArgumentNullException"/> 并带上自定义消息, 否则原样返回。
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="message">自定义异常消息</param>
    /// <returns>检查通过的原值</returns>
    public static T NotNull<T>(
        [System.Diagnostics.CodeAnalysis.NotNull] T? value,
        string parameterName,
        string message
    )
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName, message);
        }

        return value;
    }

    /// <summary>
    /// 字符串为 null 或长度越界时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="maxLength">允许的最大长度</param>
    /// <param name="minLength">允许的最小长度, 0 表示不限制</param>
    /// <returns>检查通过的字符串</returns>
    public static string NotNull(
        [System.Diagnostics.CodeAnalysis.NotNull] string? value,
        string parameterName,
        int maxLength = int.MaxValue,
        int minLength = 0
    )
    {
        if (value == null)
        {
            throw new ArgumentException($"{parameterName} can not be null!", parameterName);
        }

        if (value.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} length must be equal to or lower than {maxLength}!",
                parameterName
            );
        }

        if (minLength > 0 && value.Length < minLength)
        {
            throw new ArgumentException(
                $"{parameterName} length must be equal to or bigger than {minLength}!",
                parameterName
            );
        }

        return value;
    }

    /// <summary>
    /// 字符串为 null、空串、纯空白或长度越界时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="maxLength">允许的最大长度</param>
    /// <param name="minLength">允许的最小长度, 0 表示不限制</param>
    /// <returns>检查通过的字符串</returns>
    public static string NotNullOrWhiteSpace(
        [System.Diagnostics.CodeAnalysis.NotNull] string? value,
        string parameterName,
        int maxLength = int.MaxValue,
        int minLength = 0
    )
    {
        if (value.IsNullOrWhiteSpace())
        {
            throw new ArgumentException(
                $"{parameterName} can not be null, empty or white space!",
                parameterName
            );
        }

        if (value!.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} length must be equal to or lower than {maxLength}!",
                parameterName
            );
        }

        if (minLength > 0 && value!.Length < minLength)
        {
            throw new ArgumentException(
                $"{parameterName} length must be equal to or bigger than {minLength}!",
                parameterName
            );
        }

        return value;
    }

    /// <summary>
    /// 字符串为 null、空串或长度越界时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="maxLength">允许的最大长度</param>
    /// <param name="minLength">允许的最小长度, 0 表示不限制</param>
    /// <returns>检查通过的字符串</returns>
    public static string NotNullOrEmpty(
        [System.Diagnostics.CodeAnalysis.NotNull] string? value,
        string parameterName,
        int maxLength = int.MaxValue,
        int minLength = 0
    )
    {
        if (value.IsNullOrEmpty())
        {
            throw new ArgumentException(
                $"{parameterName} can not be null or empty!",
                parameterName
            );
        }

        if (value!.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} length must be equal to or lower than {maxLength}!",
                parameterName
            );
        }

        if (minLength > 0 && value!.Length < minLength)
        {
            throw new ArgumentException(
                $"{parameterName} length must be equal to or bigger than {minLength}!",
                parameterName
            );
        }

        return value;
    }

    /// <summary>
    /// 集合为 null 或没有元素时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="value">要检查的集合</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的集合</returns>
    public static ICollection<T> NotNullOrEmpty<T>(
        [System.Diagnostics.CodeAnalysis.NotNull] ICollection<T>? value,
        string parameterName
    )
    {
        if (value == null || value.Count <= 0)
        {
            throw new ArgumentException(
                parameterName + " can not be null or empty!",
                parameterName
            );
        }

        return value;
    }

    /// <summary>
    /// 类型不能赋值给 <typeparamref name="TBaseType"/> 时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <typeparam name="TBaseType">期望的基类型</typeparam>
    /// <param name="type">要检查的类型</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的类型</returns>
    public static Type AssignableTo<TBaseType>(Type type, string parameterName)
    {
        NotNull(type, parameterName);

        if (!type.IsAssignableTo<TBaseType>())
        {
            throw new ArgumentException(
                $"{parameterName} (type of {type.AssemblyQualifiedName}) should be assignable to the {typeof(TBaseType).GetFullNameWithAssemblyName()}!"
            );
        }

        return type;
    }

    /// <summary>
    /// 只做长度校验: 给了最小长度时不允许 null 或空串, 超过最大长度时同样抛 <see cref="ArgumentException"/>。
    /// </summary>
    /// <param name="value">要检查的字符串, 可以为 null</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="maxLength">允许的最大长度</param>
    /// <param name="minLength">允许的最小长度, 0 表示不限制</param>
    /// <returns>检查通过的字符串</returns>
    public static string? Length(
        string? value,
        string parameterName,
        int maxLength,
        int minLength = 0
    )
    {
        if (minLength > 0)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    parameterName + " can not be null or empty!",
                    parameterName
                );
            }

            if (value!.Length < minLength)
            {
                throw new ArgumentException(
                    $"{parameterName} length must be equal to or bigger than {minLength}!",
                    parameterName
                );
            }
        }

        if (value != null && value.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} length must be equal to or lower than {maxLength}!",
                parameterName
            );
        }

        return value;
    }

    /// <summary>
    /// 值小于等于 0 时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的值</returns>
    public static Int16 Positive(Int16 value, string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }
        else if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }
        return value;
    }

    /// <summary>
    /// 值小于等于 0 时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的值</returns>
    public static Int32 Positive(Int32 value, string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }
        else if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }
        return value;
    }

    /// <summary>
    /// 值小于等于 0 时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的值</returns>
    public static Int64 Positive(Int64 value, string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }
        else if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }
        return value;
    }

    /// <summary>
    /// 值小于等于 0 时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的值</returns>
    public static float Positive(float value, string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }
        else if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }
        return value;
    }

    /// <summary>
    /// 值小于等于 0 时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的值</returns>
    public static double Positive(double value, string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }
        else if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }
        return value;
    }

    /// <summary>
    /// 值小于等于 0 时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的值</returns>
    public static decimal Positive(decimal value, string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }
        else if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }
        return value;
    }

    /// <summary>
    /// 值超出给定区间时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="minimumValue">区间下限</param>
    /// <param name="maximumValue">区间上限</param>
    /// <returns>检查通过的值</returns>
    public static Int16 Range(
        Int16 value,
        string parameterName,
        Int16 minimumValue,
        Int16 maximumValue = Int16.MaxValue
    )
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException(
                $"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}"
            );
        }

        return value;
    }

    /// <summary>
    /// 值超出给定区间时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="minimumValue">区间下限</param>
    /// <param name="maximumValue">区间上限</param>
    /// <returns>检查通过的值</returns>
    public static Int32 Range(
        Int32 value,
        string parameterName,
        Int32 minimumValue,
        Int32 maximumValue = Int32.MaxValue
    )
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException(
                $"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}"
            );
        }

        return value;
    }

    /// <summary>
    /// 值超出给定区间时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="minimumValue">区间下限</param>
    /// <param name="maximumValue">区间上限</param>
    /// <returns>检查通过的值</returns>
    public static Int64 Range(
        Int64 value,
        string parameterName,
        Int64 minimumValue,
        Int64 maximumValue = Int64.MaxValue
    )
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException(
                $"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}"
            );
        }

        return value;
    }

    /// <summary>
    /// 值超出给定区间时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="minimumValue">区间下限</param>
    /// <param name="maximumValue">区间上限</param>
    /// <returns>检查通过的值</returns>
    public static float Range(
        float value,
        string parameterName,
        float minimumValue,
        float maximumValue = float.MaxValue
    )
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException(
                $"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}"
            );
        }
        return value;
    }

    /// <summary>
    /// 值超出给定区间时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="minimumValue">区间下限</param>
    /// <param name="maximumValue">区间上限</param>
    /// <returns>检查通过的值</returns>
    public static double Range(
        double value,
        string parameterName,
        double minimumValue,
        double maximumValue = double.MaxValue
    )
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException(
                $"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}"
            );
        }

        return value;
    }

    /// <summary>
    /// 值超出给定区间时抛 <see cref="ArgumentException"/>, 否则原样返回。
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <param name="minimumValue">区间下限</param>
    /// <param name="maximumValue">区间上限</param>
    /// <returns>检查通过的值</returns>
    public static decimal Range(
        decimal value,
        string parameterName,
        decimal minimumValue,
        decimal maximumValue = decimal.MaxValue
    )
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException(
                $"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}"
            );
        }

        return value;
    }

    /// <summary>
    /// 值为 null 或等于该类型的默认值时抛 <see cref="ArgumentException"/>, 否则返回拆包后的值。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="value">要检查的可空值</param>
    /// <param name="parameterName">参数名, 会写进异常信息</param>
    /// <returns>检查通过的值</returns>
    public static T NotDefaultOrNull<T>(
        [System.Diagnostics.CodeAnalysis.NotNull] T? value,
        string parameterName
    )
        where T : struct
    {
        if (value == null)
        {
            throw new ArgumentException($"{parameterName} is null!", parameterName);
        }

        if (value.Value.Equals(default(T)))
        {
            throw new ArgumentException($"{parameterName} has a default value!", parameterName);
        }

        return value.Value;
    }
}
