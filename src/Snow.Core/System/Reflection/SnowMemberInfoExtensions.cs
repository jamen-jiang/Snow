using System.Linq;

namespace System.Reflection;

/// <summary>
/// 针对 <see cref="MemberInfo"/> 的扩展方法。
/// </summary>
public static class SnowMemberInfoExtensions
{
    /// <summary>
    /// 取成员上的单个特性: 同一个特性标了多次时只返回第一个。
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="memberInfo">要在其上查找特性的成员</param>
    /// <param name="inherit">是否包含继承来的特性</param>
    /// <returns>找到时返回特性实例, 找不到返回 null。</returns>
    public static TAttribute? GetSingleAttributeOrNull<TAttribute>(this MemberInfo memberInfo, bool inherit = true)
        where TAttribute : Attribute
    {
        if (memberInfo == null)
        {
            throw new ArgumentNullException(nameof(memberInfo));
        }

        var attrs = memberInfo.GetCustomAttributes(typeof(TAttribute), inherit).ToArray();
        if (attrs.Length > 0)
        {
            return (TAttribute)attrs[0];
        }

        return default;
    }


    /// <summary>
    /// 在类型自身及其全部基类上查找特性: 自己没标时才往基类找, 找到第一个就返回。
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="type">要在其上查找特性的类型</param>
    /// <param name="inherit">是否包含继承来的特性</param>
    /// <returns>找到时返回特性实例, 自己和基类都没有则返回 null。</returns>
    public static TAttribute? GetSingleAttributeOfTypeOrBaseTypesOrNull<TAttribute>(this Type type, bool inherit = true)
        where TAttribute : Attribute
    {
        var attr = type.GetTypeInfo().GetSingleAttributeOrNull<TAttribute>();
        if (attr != null)
        {
            return attr;
        }

        if (type.GetTypeInfo().BaseType == null)
        {
            return null;
        }

        return type.GetTypeInfo().BaseType?.GetSingleAttributeOfTypeOrBaseTypesOrNull<TAttribute>(inherit);
    }
}
