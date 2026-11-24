using Microsoft.Extensions.Options;

namespace Snow.Options;

/// <summary>
/// 与 Microsoft 的 UnnamedOptionsManager 类似, 但不使用锁; 对应 ABP 的 <c>AbpUnnamedOptionsManager</c>。
/// <para>用于避免在多个线程同时解析选项时发生死锁。</para>
/// </summary>
public class SnowUnnamedOptionsManager<TOptions> : IOptions<TOptions>
    where TOptions : class
{
    private readonly IOptionsFactory<TOptions> _factory;
    private TOptions? _value;

    public SnowUnnamedOptionsManager(IOptionsFactory<TOptions> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public TOptions Value
    {
        get
        {
            if (_value is { } value)
            {
                return value;
            }

            _value = _factory.Create(Microsoft.Extensions.Options.Options.DefaultName);
            return _value;
        }
    }
}
