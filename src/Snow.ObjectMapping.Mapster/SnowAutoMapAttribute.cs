namespace Snow.ObjectMapping.Mapster;

/// <summary>
/// 声明式映射标注: 打在源/目标类型上, 模块启动时自动建映射, 对应 ABP 的 <c>AutoMapAttribute</c>
/// <para>等价于 Mapster 官方的 <c>[AdaptTo]</c> / <c>[AdaptFrom]</c> / <c>[AdaptTwoWays]</c>, 但方向与目标类型写在同一个注解里, 且不要求类型自己引用 Mapster。</para>
/// <para>同一个类可以打多次, 例如同时声明到 DTO 与到 Input 的映射。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class SnowAutoMapAttribute : Attribute
{
    /// <summary>
    /// 构造器: 默认从标注类型映射到给定类型(方向 <see cref="SnowAutoMapDirection.To"/>)
    /// </summary>
    /// <param name="targetType">映射的目标类型</param>
    public SnowAutoMapAttribute(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        Types = [targetType];
        Direction = SnowAutoMapDirection.To;
    }

    /// <summary>
    /// 构造器: 显式指定方向与一个或多个对端类型
    /// </summary>
    /// <param name="direction">映射方向</param>
    /// <param name="types">对端类型, 方向为 <see cref="SnowAutoMapDirection.To"/> 时是目标类型, 为 <see cref="SnowAutoMapDirection.From"/> 时是源类型</param>
    public SnowAutoMapAttribute(SnowAutoMapDirection direction, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(types);

        if (types.Length == 0)
        {
            throw new ArgumentException("至少要给一个对端类型。", nameof(types));
        }

        Types = types;
        Direction = direction;
    }

    /// <summary>
    /// 对端类型
    /// </summary>
    public Type[] Types { get; }

    /// <summary>
    /// 映射方向
    /// </summary>
    public SnowAutoMapDirection Direction { get; }
}
