using Snow.DependencyInjection;

namespace Snow.Settings;

/// <summary>
/// 空的设置存储: 默认实现, 一律返回 null(等于"没有保存过任何设置")
/// </summary>
[SingletonService]
public sealed class NullSettingStore : ISettingStore
{
    /// <inheritdoc />
    public Task<string?> GetOrNullAsync(string name, string? providerName = null, string? providerKey = null, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
