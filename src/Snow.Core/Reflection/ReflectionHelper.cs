using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Snow.Reflection;

/// <summary>
/// 反射辅助方法(对应 ABP 的 <c>Volo.Abp.Reflection.ReflectionHelper</c>)
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// 判断 <paramref name="givenType"/> 是否实现或继承了泛型类型 <paramref name="genericType"/>。
    /// </summary>
    /// <param name="givenType">要检查的类型</param>
    /// <param name="genericType">泛型类型</param>
    /// <returns>实现或继承了该泛型类型返回 true, 否则返回 false</returns>
    public static bool IsAssignableToGenericType(Type givenType, Type genericType)
    {
        var givenTypeInfo = givenType.GetTypeInfo();

        if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
        {
            return true;
        }

        foreach (var interfaceType in givenTypeInfo.GetInterfaces())
        {
            if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
            {
                return true;
            }
        }

        if (givenTypeInfo.BaseType == null)
        {
            return false;
        }

        return IsAssignableToGenericType(givenTypeInfo.BaseType, genericType);
    }

    /// <summary>
    /// 获取 <paramref name="givenType"/> 实现或继承的所有泛型类型实例(逐层递归基类与接口)。
    /// </summary>
    /// <param name="givenType">要检查的类型</param>
    /// <param name="genericType">泛型类型</param>
    /// <returns>匹配到的泛型类型实例列表</returns>
    public static List<Type> GetImplementedGenericTypes(Type givenType, Type genericType)
    {
        var result = new List<Type>();
        AddImplementedGenericTypes(result, givenType, genericType);
        return result;
    }

    /// <summary>
    /// 递归收集实现或继承了指定泛型类型的所有类型。
    /// </summary>
    /// <param name="result">收集结果的容器</param>
    /// <param name="givenType">要检查的类型</param>
    /// <param name="genericType">泛型类型</param>
    private static void AddImplementedGenericTypes(List<Type> result, Type givenType, Type genericType)
    {
        var givenTypeInfo = givenType.GetTypeInfo();

        if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
        {
            result.AddIfNotContains(givenType);
        }

        foreach (var interfaceType in givenTypeInfo.GetInterfaces())
        {
            if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
            {
                result.AddIfNotContains(interfaceType);
            }
        }

        if (givenTypeInfo.BaseType == null)
        {
            return;
        }

        AddImplementedGenericTypes(result, givenTypeInfo.BaseType, genericType);
    }

    /// <summary>
    /// 尝试获取成员上声明的单个特性; 取不到时返回 <paramref name="defaultValue"/>。
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="memberInfo">成员信息</param>
    /// <param name="defaultValue">默认值(默认 null)</param>
    /// <param name="inherit">是否从基类继承特性</param>
    /// <returns>找到的特性实例; 没有时返回 <paramref name="defaultValue"/></returns>
    public static TAttribute? GetSingleAttributeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute? defaultValue = default, bool inherit = true)
        where TAttribute : Attribute
    {
        // 先取成员本身的特性
        if (memberInfo.IsDefined(typeof(TAttribute), inherit))
        {
            return memberInfo.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().First();
        }

        return defaultValue;
    }

    /// <summary>
    /// 尝试获取成员或其声明类型(含继承)上的单个特性; 都没有时返回 <paramref name="defaultValue"/>。
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="memberInfo">成员信息</param>
    /// <param name="defaultValue">默认值(默认 null)</param>
    /// <param name="inherit">是否从基类继承特性</param>
    /// <returns>找到的特性实例; 没有时返回 <paramref name="defaultValue"/></returns>
    public static TAttribute? GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute? defaultValue = default, bool inherit = true)
        where TAttribute : class
    {
        return memberInfo.GetCustomAttributes(true).OfType<TAttribute>().FirstOrDefault()
               ?? memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes(true).OfType<TAttribute>().FirstOrDefault()
               ?? defaultValue;
    }

    /// <summary>
    /// 获取成员及其声明类型(含继承)上声明的所有特性并去重。
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="memberInfo">成员信息</param>
    /// <param name="inherit">是否从基类继承特性</param>
    /// <returns>成员及声明类型上的特性集合</returns>
    public static IEnumerable<TAttribute> GetAttributesOfMemberOrDeclaringType<TAttribute>(MemberInfo memberInfo, bool inherit = true)
        where TAttribute : class
    {
        var customAttributes = memberInfo.GetCustomAttributes(true).OfType<TAttribute>();
        var declaringTypeCustomAttributes =
            memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes(true).OfType<TAttribute>();
        return declaringTypeCustomAttributes != null
            ? customAttributes.Concat(declaringTypeCustomAttributes).Distinct()
            : customAttributes;
    }

    /// <summary>
    /// 按属性路径(如 "A.B.C")从给定对象上取得属性值。
    /// </summary>
    /// <param name="obj">源对象</param>
    /// <param name="objectType">对象类型</param>
    /// <param name="propertyPath">属性路径, 以 '.' 分隔</param>
    /// <returns>取到的属性值; 路径无效时返回 null</returns>
    public static object? GetValueByPath(object obj, Type objectType, string propertyPath)
    {
        var value = obj;
        var currentType = objectType;
        var objectPath = currentType.FullName;
        var absolutePropertyPath = propertyPath;
        if (objectPath != null && absolutePropertyPath.StartsWith(objectPath))
        {
            absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");
        }

        foreach (var propertyName in absolutePropertyPath.Split('.'))
        {
            var property = currentType.GetProperty(propertyName);
            if (property != null)
            {
                if (value != null)
                {
                    value = property.GetValue(value, null);
                }
                currentType = property.PropertyType;
            }
            else
            {
                value = null;
                break;
            }
        }

        return value;
    }

    /// <summary>
    /// 按属性路径(如 "A.B.C")把值写入给定对象。
    /// </summary>
    /// <param name="obj">目标对象</param>
    /// <param name="objectType">对象类型</param>
    /// <param name="propertyPath">属性路径, 以 '.' 分隔</param>
    /// <param name="value">要写入的值</param>
    internal static void SetValueByPath(object obj, Type objectType, string propertyPath, object value)
    {
        var currentType = objectType;
        PropertyInfo property;
        var objectPath = currentType.FullName!;
        var absolutePropertyPath = propertyPath;
        if (absolutePropertyPath.StartsWith(objectPath))
        {
            absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");
        }

        var properties = absolutePropertyPath.Split('.');

        if (properties.Length == 1)
        {
            property = objectType.GetProperty(properties.First())!;
            property.SetValue(obj, value);
            return;
        }

        for (int i = 0; i < properties.Length - 1; i++)
        {
            property = currentType.GetProperty(properties[i])!;
            obj = property.GetValue(obj, null)!;
            currentType = property.PropertyType;
        }

        property = currentType.GetProperty(properties.Last())!;
        property.SetValue(obj, value);
    }

    /// <summary>
    /// 递归获取指定类型(含基类型与嵌套类型)中所有 public 常量字段的值。
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>所有 public 常量字段的值数组</returns>
    public static string[] GetPublicConstantsRecursively(Type type)
    {
        const int maxRecursiveParameterValidationDepth = 8;

        var publicConstants = new List<string>();

        void Recursively(List<string> constants, Type targetType, int currentDepth)
        {
            if (currentDepth > maxRecursiveParameterValidationDepth)
            {
                return;
            }

            constants.AddRange(targetType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(x => x.IsLiteral && !x.IsInitOnly)
                .Select(x => x.GetValue(null)!.ToString()!));

            var nestedTypes = targetType.GetNestedTypes(BindingFlags.Public);

            foreach (var nestedType in nestedTypes)
            {
                Recursively(constants, nestedType, currentDepth + 1);
            }
        }

        Recursively(publicConstants, type, 1);

        return publicConstants.ToArray();
    }

    /// <summary>
    /// 判断属性是否可空, 包含可空引用类型(NRT)。
    /// </summary>
    /// <param name="propertyInfo">要检查的属性</param>
    /// <returns>可空返回 true, 否则返回 false</returns>
    public static bool IsNullable(PropertyInfo propertyInfo)
    {
        if (TypeHelper.IsNullable(propertyInfo.PropertyType))
        {
            return true;
        }

#if NET6_0_OR_GREATER
        var nullabilityInfoContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityInfoContext.Create(propertyInfo);
        return nullabilityInfo.ReadState == NullabilityState.Nullable;
#else
        var attr = propertyInfo.GetCustomAttributes().FirstOrDefault(a => a.GetType().FullName == "System.Runtime.CompilerServices.NullableAttribute");
        if (attr != null)
        {
            var getter = NullableGetterCache.GetOrAdd(attr.GetType(), CreateNullableAccessor);
            return getter(attr)?[0] == 2;
        }
        return false;
#endif
    }

#if !NET6_0_OR_GREATER
    // 特性类型 → 读取 NullableFlags 的委托缓存(仅旧目标框架使用)
    private static readonly ConcurrentDictionary<Type, Func<object, byte[]?>> NullableGetterCache = new();

    /// <summary>
    /// 为指定特性类型创建读取 NullableFlags 的委托(仅旧目标框架使用)。
    /// </summary>
    /// <param name="attrType">特性类型</param>
    /// <returns>读取 NullableFlags 的委托</returns>
    private static Func<object, byte[]?> CreateNullableAccessor(Type attrType)
    {
        var param = Expression.Parameter(typeof(object), "attr");
        var casted = Expression.Convert(param, attrType);

        var flagsField = attrType.GetField("NullableFlags");
        if (flagsField == null)
        {
            return _ => null;
        }

        var access = Expression.Field(casted, flagsField);
        return Expression.Lambda<Func<object, byte[]?>>(access, param).Compile();
    }
#endif
}
