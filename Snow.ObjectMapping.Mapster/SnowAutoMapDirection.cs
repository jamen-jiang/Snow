namespace Snow.ObjectMapping.Mapster;

/// <summary>
/// 声明式映射的方向: 对应 ABP 的 <c>AutoMapDirection</c>
/// </summary>
public enum SnowAutoMapDirection
{
    /// <summary>
    /// 从标注 <see cref="SnowAutoMapAttribute"/> 的类型映射到参数里的类型
    /// </summary>
    To = 0,

    /// <summary>
    /// 从参数里的类型映射到标注 <see cref="SnowAutoMapAttribute"/> 的类型
    /// </summary>
    From = 1,

    /// <summary>
    /// 双向都建映射
    /// </summary>
    TwoWays = 2
}
