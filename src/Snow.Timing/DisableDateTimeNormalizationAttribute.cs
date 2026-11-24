using System;

namespace Snow.Timing;

/// <summary>
/// 标注在此处的日期时间不做时区归一化(对应 ABP 的 <c>DisableDateTimeNormalizationAttribute</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
public class DisableDateTimeNormalizationAttribute : Attribute
{
}
