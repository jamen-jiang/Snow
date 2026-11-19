using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using BclDistributedCache = Microsoft.Extensions.Caching.Distributed.IDistributedCache;

namespace Rongia.Caching;

/// <summary>
/// 强类型分布式缓存实现(键为字符串): 转发给双泛型实现
/// </summary>
/// <typeparam name="TCacheItem">缓存项类型</typeparam>
public class DistributedCache<TCacheItem> : IDistributedCache<TCacheItem>
    where TCacheItem : class
{
    /// <summary>
    /// 内部实现(键类型为字符串)
    /// </summary>
    public IDistributedCache<TCacheItem, string> InternalCache { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="internalCache">内部实现</param>
    public DistributedCache(IDistributedCache<TCacheItem, string> internalCache)
    {
        ArgumentNullException.ThrowIfNull(internalCache);

        InternalCache = internalCache;
    }

    /// <inheritdoc />
    public TCacheItem? Get(string key, bool? hideErrors = null) => InternalCache.Get(key, hideErrors);

    /// <inheritdoc />
    public KeyValuePair<string, TCacheItem?>[] GetMany(IEnumerable<string> keys, bool? hideErrors = null)
        => InternalCache.GetMany(keys, hideErrors);

    /// <inheritdoc />
    public Task<KeyValuePair<string, TCacheItem?>[]> GetManyAsync(IEnumerable<string> keys, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.GetManyAsync(keys, hideErrors, token);

    /// <inheritdoc />
    public Task<TCacheItem?> GetAsync(string key, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.GetAsync(key, hideErrors, token);

    /// <inheritdoc />
    public TCacheItem? GetOrAdd(string key, Func<TCacheItem> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null)
        => InternalCache.GetOrAdd(key, factory, optionsFactory, hideErrors);

    /// <inheritdoc />
    public Task<TCacheItem?> GetOrAddAsync(string key, Func<Task<TCacheItem>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.GetOrAddAsync(key, factory, optionsFactory, hideErrors, token);

    /// <inheritdoc />
    public KeyValuePair<string, TCacheItem?>[] GetOrAddMany(IEnumerable<string> keys, Func<IEnumerable<string>, List<KeyValuePair<string, TCacheItem>>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null)
        => InternalCache.GetOrAddMany(keys, factory, optionsFactory, hideErrors);

    /// <inheritdoc />
    public Task<KeyValuePair<string, TCacheItem?>[]> GetOrAddManyAsync(IEnumerable<string> keys, Func<IEnumerable<string>, Task<List<KeyValuePair<string, TCacheItem>>>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.GetOrAddManyAsync(keys, factory, optionsFactory, hideErrors, token);

    /// <inheritdoc />
    public void Set(string key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null)
        => InternalCache.Set(key, value, options, hideErrors);

    /// <inheritdoc />
    public Task SetAsync(string key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.SetAsync(key, value, options, hideErrors, token);

    /// <inheritdoc />
    public void SetMany(IEnumerable<KeyValuePair<string, TCacheItem>> items, DistributedCacheEntryOptions? options = null, bool? hideErrors = null)
        => InternalCache.SetMany(items, options, hideErrors);

    /// <inheritdoc />
    public Task SetManyAsync(IEnumerable<KeyValuePair<string, TCacheItem>> items, DistributedCacheEntryOptions? options = null, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.SetManyAsync(items, options, hideErrors, token);

    /// <inheritdoc />
    public void Refresh(string key, bool? hideErrors = null) => InternalCache.Refresh(key, hideErrors);

    /// <inheritdoc />
    public Task RefreshAsync(string key, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.RefreshAsync(key, hideErrors, token);

    /// <inheritdoc />
    public void RefreshMany(IEnumerable<string> keys, bool? hideErrors = null) => InternalCache.RefreshMany(keys, hideErrors);

    /// <inheritdoc />
    public Task RefreshManyAsync(IEnumerable<string> keys, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.RefreshManyAsync(keys, hideErrors, token);

    /// <inheritdoc />
    public void Remove(string key, bool? hideErrors = null) => InternalCache.Remove(key, hideErrors);

    /// <inheritdoc />
    public Task RemoveAsync(string key, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.RemoveAsync(key, hideErrors, token);

    /// <inheritdoc />
    public void RemoveMany(IEnumerable<string> keys, bool? hideErrors = null) => InternalCache.RemoveMany(keys, hideErrors);

    /// <inheritdoc />
    public Task RemoveManyAsync(IEnumerable<string> keys, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.RemoveManyAsync(keys, hideErrors, token);

    /// <inheritdoc />
    public void Clear(bool? hideErrors = null) => InternalCache.Clear(hideErrors);

    /// <inheritdoc />
    public Task ClearAsync(bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.ClearAsync(hideErrors, token);
}
/// <summary>
/// 强类型分布式缓存实现: 键规范化、序列化、防击穿互斥、异常隔离与默认条目策略
/// <para>缓存名由 <see cref="CacheNameAttribute.GetCacheName"/> 从 <typeparamref name="TCacheItem"/> 推导。</para>
/// </summary>
/// <typeparam name="TCacheItem">缓存项类型</typeparam>
/// <typeparam name="TCacheKey">缓存键类型</typeparam>
public class DistributedCache<TCacheItem, TCacheKey> : IDistributedCache<TCacheItem, TCacheKey>
    where TCacheItem : class
    where TCacheKey : notnull
{
    private readonly DistributedCacheOptions _options;
    private readonly BclDistributedCache _cache;
    private readonly ILogger<DistributedCache<TCacheItem, TCacheKey>> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">分布式缓存配置</param>
    /// <param name="cache">底层字节缓存(内存或 Redis)</param>
    /// <param name="logger">日志</param>
    public DistributedCache(IOptions<DistributedCacheOptions> options, BclDistributedCache cache, ILogger<DistributedCache<TCacheItem, TCacheKey>>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cache);

        _options = options.Value;
        _cache = cache;
        _logger = logger ?? NullLogger<DistributedCache<TCacheItem, TCacheKey>>.Instance;
        CacheName = CacheNameAttribute.GetCacheName(typeof(TCacheItem));
        SyncSemaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// 逻辑缓存名(由缓存项类型推导)
    /// </summary>
    protected string CacheName { get; }

    /// <summary>
    /// GetOrAdd 的进程内互斥, 避免同一实例内的缓存击穿
    /// </summary>
    protected SemaphoreSlim SyncSemaphore { get; }

    /// <inheritdoc />
    public TCacheItem? Get(TCacheKey key, bool? hideErrors = null)
    {
        try
        {
            byte[]? bytes = _cache.Get(DataKey(key));

            return bytes is null ? null : JsonSerializer.Deserialize<TCacheItem>(bytes, DistributedCacheOptions.SerializerOptions);
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TCacheItem?> GetAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default)
    {
        try
        {
            byte[]? bytes = await _cache.GetAsync(DataKey(key), token).ConfigureAwait(false);

            return bytes is null ? null : JsonSerializer.Deserialize<TCacheItem>(bytes, DistributedCacheOptions.SerializerOptions);
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
            return null;
        }
    }

    /// <inheritdoc />
    public KeyValuePair<TCacheKey, TCacheItem?>[] GetMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null)
    {
        ArgumentNullException.ThrowIfNull(keys);

        return keys.Select(key => new KeyValuePair<TCacheKey, TCacheItem?>(key, Get(key, hideErrors))).ToArray();
    }

    /// <inheritdoc />
    public async Task<KeyValuePair<TCacheKey, TCacheItem?>[]> GetManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        List<KeyValuePair<TCacheKey, TCacheItem?>> results = new();

        foreach (TCacheKey key in keys)
        {
            results.Add(new KeyValuePair<TCacheKey, TCacheItem?>(key, await GetAsync(key, hideErrors, token).ConfigureAwait(false)));
        }

        return results.ToArray();
    }

    /// <inheritdoc />
    public TCacheItem? GetOrAdd(TCacheKey key, Func<TCacheItem> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        TCacheItem? value = Get(key, hideErrors);

        if (value is not null)
        {
            return value;
        }

        // 进程内互斥: 只有一个线程去执行工厂, 其余线程等它写完后再读一次缓存
        SyncSemaphore.Wait();

        try
        {
            value = Get(key, hideErrors);

            if (value is not null)
            {
                return value;
            }

            value = factory();

            if (value is not null)
            {
                Set(key, value, optionsFactory?.Invoke(), hideErrors);
            }

            return value;
        }
        finally
        {
            SyncSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TCacheItem?> GetOrAddAsync(TCacheKey key, Func<Task<TCacheItem>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        TCacheItem? value = await GetAsync(key, hideErrors, token).ConfigureAwait(false);

        if (value is not null)
        {
            return value;
        }

        await SyncSemaphore.WaitAsync(token).ConfigureAwait(false);

        try
        {
            value = await GetAsync(key, hideErrors, token).ConfigureAwait(false);

            if (value is not null)
            {
                return value;
            }

            value = await factory().ConfigureAwait(false);

            if (value is not null)
            {
                await SetAsync(key, value, optionsFactory?.Invoke(), hideErrors, token).ConfigureAwait(false);
            }

            return value;
        }
        finally
        {
            SyncSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public KeyValuePair<TCacheKey, TCacheItem?>[] GetOrAddMany(IEnumerable<TCacheKey> keys, Func<IEnumerable<TCacheKey>, List<KeyValuePair<TCacheKey, TCacheItem>>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(factory);

        List<TCacheKey> requested = keys.ToList();

        Dictionary<TCacheKey, TCacheItem?> found = new(requested.Count);

        List<TCacheKey> missing = new();

        foreach (TCacheKey key in requested)
        {
            TCacheItem? value = Get(key, hideErrors);

            if (value is null)
            {
                missing.Add(key);
            }
            else
            {
                found[key] = value;
            }
        }

        if (missing.Count > 0)
        {
            List<KeyValuePair<TCacheKey, TCacheItem>> created = factory(missing);

            DistributedCacheEntryOptions? options = optionsFactory?.Invoke();

            foreach (KeyValuePair<TCacheKey, TCacheItem> pair in created)
            {
                found[pair.Key] = pair.Value;
                Set(pair.Key, pair.Value, options, hideErrors);
            }
        }

        return requested.Select(key => new KeyValuePair<TCacheKey, TCacheItem?>(key, found.TryGetValue(key, out TCacheItem? item) ? item : null)).ToArray();
    }

    /// <inheritdoc />
    public async Task<KeyValuePair<TCacheKey, TCacheItem?>[]> GetOrAddManyAsync(IEnumerable<TCacheKey> keys, Func<IEnumerable<TCacheKey>, Task<List<KeyValuePair<TCacheKey, TCacheItem>>>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(factory);

        List<TCacheKey> requested = keys.ToList();

        Dictionary<TCacheKey, TCacheItem?> found = new(requested.Count);

        List<TCacheKey> missing = new();

        foreach (TCacheKey key in requested)
        {
            TCacheItem? value = await GetAsync(key, hideErrors, token).ConfigureAwait(false);

            if (value is null)
            {
                missing.Add(key);
            }
            else
            {
                found[key] = value;
            }
        }

        if (missing.Count > 0)
        {
            List<KeyValuePair<TCacheKey, TCacheItem>> created = await factory(missing).ConfigureAwait(false);

            DistributedCacheEntryOptions? options = optionsFactory?.Invoke();

            foreach (KeyValuePair<TCacheKey, TCacheItem> pair in created)
            {
                found[pair.Key] = pair.Value;
                await SetAsync(pair.Key, pair.Value, options, hideErrors, token).ConfigureAwait(false);
            }
        }

        return requested.Select(key => new KeyValuePair<TCacheKey, TCacheItem?>(key, found.TryGetValue(key, out TCacheItem? item) ? item : null)).ToArray();
    }

    /// <inheritdoc />
    public void Set(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, DistributedCacheOptions.SerializerOptions);

            _cache.Set(DataKey(key), payload, options ?? GetDefaultCacheEntryOptions());
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, DistributedCacheOptions.SerializerOptions);

            await _cache.SetAsync(DataKey(key), payload, options ?? GetDefaultCacheEntryOptions(), token).ConfigureAwait(false);
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
        }
    }

    /// <inheritdoc />
    public void SetMany(IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items, DistributedCacheEntryOptions? options = null, bool? hideErrors = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (KeyValuePair<TCacheKey, TCacheItem> pair in items)
        {
            Set(pair.Key, pair.Value, options, hideErrors);
        }
    }

    /// <inheritdoc />
    public async Task SetManyAsync(IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items, DistributedCacheEntryOptions? options = null, bool? hideErrors = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (KeyValuePair<TCacheKey, TCacheItem> pair in items)
        {
            await SetAsync(pair.Key, pair.Value, options, hideErrors, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Refresh(TCacheKey key, bool? hideErrors = null)
    {
        try
        {
            _cache.Refresh(DataKey(key));
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
        }
    }

    /// <inheritdoc />
    public async Task RefreshAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default)
    {
        try
        {
            await _cache.RefreshAsync(DataKey(key), token).ConfigureAwait(false);
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
        }
    }

    /// <inheritdoc />
    public void RefreshMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (TCacheKey key in keys)
        {
            Refresh(key, hideErrors);
        }
    }

    /// <inheritdoc />
    public async Task RefreshManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (TCacheKey key in keys)
        {
            await RefreshAsync(key, hideErrors, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Remove(TCacheKey key, bool? hideErrors = null)
    {
        try
        {
            _cache.Remove(DataKey(key));
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default)
    {
        try
        {
            await _cache.RemoveAsync(DataKey(key), token).ConfigureAwait(false);
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
        }
    }

    /// <inheritdoc />
    public void RemoveMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (TCacheKey key in keys)
        {
            Remove(key, hideErrors);
        }
    }

    /// <inheritdoc />
    public async Task RemoveManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (TCacheKey key in keys)
        {
            await RemoveAsync(key, hideErrors, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Clear(bool? hideErrors = null)
    {
        try
        {
            DistributedCacheKey.BumpVersion(_cache, CacheName);
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync(bool? hideErrors = null, CancellationToken token = default)
    {
        try
        {
            await DistributedCacheKey.BumpVersionAsync(_cache, CacheName, token).ConfigureAwait(false);
        }
        catch (Exception exception) when (HandleException(exception, hideErrors))
        {
        }
    }

    /// <summary>
    /// 键规范化: 缓存名 + 全局键前缀 + 代际 + 业务键
    /// </summary>
    /// <param name="key">业务键</param>
    /// <returns>底层存储键</returns>
    protected virtual string NormalizeKey(TCacheKey key) => DataKey(key);

    /// <summary>
    /// 条目选项: 先问按缓存名定制的配置器, 都没命中则用全局默认
    /// </summary>
    /// <returns>条目选项</returns>
    protected virtual DistributedCacheEntryOptions GetDefaultCacheEntryOptions() => DistributedCacheKey.GetDefaultCacheEntryOptions(_options, CacheName);

    private string DataKey(TCacheKey key) => DistributedCacheKey.DataKey(CacheName, _options.KeyPrefix, DistributedCacheKey.ReadVersion(_cache, CacheName), key.ToString() ?? string.Empty);

    /// <summary>
    /// 异常处置: 缓存故障不应影响主流程, 返回 true 表示已吞掉
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="hideErrors">是否吞掉; 为 null 时取全局配置</param>
    /// <returns>是否吞掉该异常</returns>
    private bool HandleException(Exception exception, bool? hideErrors) => DistributedCacheKey.HandleException(exception, hideErrors, _options, _logger, CacheName);
}