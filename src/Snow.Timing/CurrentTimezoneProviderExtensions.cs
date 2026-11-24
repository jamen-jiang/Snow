using System;

namespace Snow.Timing;

/// <summary>
/// <see cref="ICurrentTimezoneProvider"/> 的临时切换扩展(对应 ABP 的 <c>CurrentTimezoneProviderExtensions</c>)
/// </summary>
public static class CurrentTimezoneProviderExtensions
{
    /// <summary>
    /// 在 <c>using</c> 块内临时切换当前时区, 释放时自动还原(支持嵌套)
    /// </summary>
    /// <param name="currentTimezoneProvider">当前时区提供器</param>
    /// <param name="timeZone">临时时区(Windows 或 IANA 时区 ID)</param>
    /// <returns>释放即还原的句柄</returns>
    public static IDisposable Change(this ICurrentTimezoneProvider currentTimezoneProvider, string? timeZone)
    {
        ArgumentNullException.ThrowIfNull(currentTimezoneProvider);

        string? parentScope = currentTimezoneProvider.TimeZone;
        currentTimezoneProvider.TimeZone = timeZone;

        return new DisposeAction<ValueTuple<ICurrentTimezoneProvider, string?>>(
            static state =>
            {
                var (provider, parent) = state;
                provider.TimeZone = parent;
            },
            (currentTimezoneProvider, parentScope));
    }
}
