namespace Snow.Validation;

/// <summary>
/// 校验相关的小工具
/// </summary>
public static class SnowValidationHelper
{
    /// <summary>
    /// 判断类型是否属于"简单类型": 这类参数不做对象校验, 交给模型绑定与特性校验
    /// </summary>
    /// <param name="type">待判断的类型</param>
    /// <returns>简单类型返回 true</returns>
    public static bool IsSimpleType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(Guid)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Uri);
    }
}
