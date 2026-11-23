using Snow.DependencyInjection;

namespace Snow.Settings;

/// <summary>
/// 全局存储提供器: 从 <see cref="ISettingStore"/> 读已持久化的设置, 优先级最高(名字 "G")
/// </summary>
[SingletonService]
public sealed class GlobalSettingValueProvider : ISettingValueProvider
{
    private readonly ISettingStore _store;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="store">设置存储</param>
    public GlobalSettingValueProvider(ISettingStore store) => _store = store;

    /// <inheritdoc />
    public string Name => "G";

    /// <inheritdoc />
    public async Task<string?> GetOrNullAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return await _store.GetOrNullAsync(definition.Name, Name, null, cancellationToken).ConfigureAwait(false);
    }
}
