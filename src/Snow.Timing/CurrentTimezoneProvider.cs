using System.Threading;
using Snow.DependencyInjection;

namespace Snow.Timing;

/// <summary>
/// 基于 <see cref="AsyncLocal{T}"/> 的当前时区提供器(对应 ABP 的 <c>CurrentTimezoneProvider</c>): 并发请求之间互不干扰
/// </summary>
[SingletonService]
public class CurrentTimezoneProvider : ICurrentTimezoneProvider
{
    private readonly AsyncLocal<string?> _currentScope = new();

    /// <inheritdoc />
    public string? TimeZone
    {
        get => _currentScope.Value;
        set => _currentScope.Value = value;
    }
}
