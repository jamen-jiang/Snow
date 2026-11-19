using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Rongia.Caching;

/// <summary>
/// 命名分布式缓存: 键格式 <c>rongia:cache:{缓存名}:v{代际}:{KeyPrefix}{业务键}</c>。
/// <para>「清空整个缓存名」用代际(version)自增实现: 版本一变, 旧键自然不可见, 无需扫描底层存储,
/// O(1) 且对 Redis/内存两种后端行为一致; 旧键由自身的过期策略回收。</para>
/// </summary>
public sealed class NamedDistributedCache
{
    private const string KeyRoot = "rongia:cache";

    private readonly DistributedCacheOptions _options;
    private readonly IDistributedCache _cache;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">缓存名</param>
    /// <param name="options">分布式缓存配置</param>
    /// <param name="cache">底层分布式缓存</param>
    public NamedDistributedCache(string name, DistributedCacheOptions options, IDistributedCache cache)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cache);

        Name = name;
        _options = options;
        _cache = cache;
    }

    /// <summary>
    /// 缓存名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 读取缓存项, 未命中返回 null
    /// </summary>
    /// <param name="key">业务键</param>
    /// <param name="type">值类型</param>
    /// <param name="token">取消令牌</param>
    /// <returns>缓存值, 未命中为 null</returns>
    public async Task<object?> GetAsync(string key, Type type, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(type);

        string version = await GetVersionAsync(token).ConfigureAwait(false);
        byte[]? bytes = await _cache.GetAsync(DataKey(version, key), token).ConfigureAwait(false);

        return bytes is null ? null : JsonSerializer.Deserialize(bytes, type, DistributedCacheOptions.SerializerOptions);
    }

    /// <summary>
    /// 写入缓存项
    /// </summary>
    /// <param name="key">业务键</param>
    /// <param name="value">缓存值</param>
    /// <param name="type">值类型</param>
    /// <param name="options">条目选项, 为 null 时用缓存默认策略</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示写入过程的任务</returns>
    public async Task SetAsync(string key, object? value, Type type, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(type);

        string version = await GetVersionAsync(token).ConfigureAwait(false);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, type, DistributedCacheOptions.SerializerOptions);

        await _cache.SetAsync(DataKey(version, key), payload, options ?? _options.GlobalCacheEntryOptions, token).ConfigureAwait(false);
    }

    /// <summary>
    /// 移除单个缓存键
    /// </summary>
    /// <param name="key">业务键</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示移除过程的任务</returns>
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        string version = await GetVersionAsync(token).ConfigureAwait(false);
        await _cache.RemoveAsync(DataKey(version, key), token).ConfigureAwait(false);
    }

    /// <summary>
    /// 清空该缓存名下的全部条目(对应 Spring 的 allEntries = true)
    /// </summary>
    /// <param name="token">取消令牌</param>
    /// <returns>表示清理过程的任务</returns>
    public async Task ClearAsync(CancellationToken token = default)
    {
        int current = int.TryParse(await GetVersionAsync(token).ConfigureAwait(false), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 1;
        await _cache.SetStringAsync(VersionKey, (current + 1).ToString(CultureInfo.InvariantCulture), token).ConfigureAwait(false);
    }

    private string VersionKey => KeyRoot + ":" + Name + ":__version__";

    private string DataKey(string version, string key) => KeyRoot + ":" + Name + ":v" + version + ":" + _options.KeyPrefix + key;

    private async Task<string> GetVersionAsync(CancellationToken token)
    {
        string? version = await _cache.GetStringAsync(VersionKey, token).ConfigureAwait(false);

        return string.IsNullOrEmpty(version) ? "1" : version;
    }
}
