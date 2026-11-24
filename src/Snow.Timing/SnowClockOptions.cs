using System;

namespace Snow.Timing;

/// <summary>
/// 时钟选项(对应 ABP 的 <c>AbpClockOptions</c>): 决定 <see cref="Clock"/> 以哪种 <see cref="DateTimeKind"/> 作为基准
/// <para>默认 <see cref="DateTimeKind.Unspecified"/>; 置为 <see cref="DateTimeKind.Utc"/> 时支持多时区并输出 UTC。</para>
/// </summary>
public class SnowClockOptions
{
    /// <summary>
    /// 时钟基准 Kind, 默认 <see cref="DateTimeKind.Unspecified"/>
    /// </summary>
    public DateTimeKind Kind { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowClockOptions()
    {
        Kind = DateTimeKind.Unspecified;
    }
}
