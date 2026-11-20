using System.ComponentModel;
using System.Reflection;

namespace System;

/// <summary>
/// 枚举扩展方法:读取成员上的 Description 描述
/// (对应 Java BaseEnum 的 getLabel 约定;code 即枚举成员名本身,不需要单独取)
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 获取枚举成员的描述:取 Description 特性值,没有时退回成员名
    /// </summary>
    public static string GetDescription<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var field = typeof(TEnum).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}
