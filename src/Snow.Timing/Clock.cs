using System;
using Snow.DependencyInjection;

namespace Snow.Timing;

/// <summary>
/// 默认时间提供器: 直接读系统时钟
/// </summary>
[SingletonService]
public class Clock : IClock
{
    /// <inheritdoc />
    public virtual DateTime Now => DateTime.Now;

    /// <inheritdoc />
    public virtual DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public virtual DateTimeOffset OffsetNow => DateTimeOffset.Now;

    /// <inheritdoc />
    public virtual DateTimeOffset OffsetUtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public virtual DateTime Normalize(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }
}
