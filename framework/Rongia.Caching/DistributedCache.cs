using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
    public Task<TCacheItem?> GetAsync(string key, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.GetAsync(key, hideErrors, token);

    /// <inheritdoc />
    public TCacheItem? GetOrAdd(string key, Func<TCacheItem> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null)
        => InternalCache.GetOrAdd(key, factory, optionsFactory, hideErrors);

    /// <inheritdoc />
    public Task<TCacheItem?> GetOrAddAsync(string key, Func<Task<TCacheItem>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.GetOrAddAsync(key, factory, optionsFactory, hideErrors, token);

    /// <inheritdoc />
    public void Set(string key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null)
        => InternalCache.Set(key, value, options, hideErrors);

    /// <inheritdoc />
    public Task SetAsync(string key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.SetAsync(key, value, options, hideErrors, token);

    /// <inheritdoc />
    public void Remove(string key, bool? hideErrors = null) => InternalCache.Remove(key, hideErrors);

    /// <inheritdoc />
    public Task RemoveAsync(string key, bool? hideErrors = null, CancellationToken token = default)
        => InternalCache.RemoveAsync(key, hideErrors, token);
}

/// <summary>
/// 强类型分布式缓存实现: 键处理、序列化、异常隔离与默认条目策略
/// </summary>
/// <typeparam name="TCacheItem">缓存项类型</typeparam>
/// <typeparam name="TCacheKey">缓存键类型</typeparam>
public class DistributedCache<TCacheItem, TCacheKey> : IDistributedCache<TCacheItem, TCacheKey>
    where TCacheItem : class
    where TCacheKey : notnull
{
    /// <summary>
    /// 日志
    /// </summary>
    public ILogger<DistributedCache<TCacheItem, TCacheKey>> Logger { get; set; }

    /// <summary>
    /// 逻辑缓存名
    /// </summary>
    protected string CacheName { get; set; } = default!;

    /// <summary>
    /// 底层分布式缓存
    /// </summary>
    protected IDistributedCache Cache { get; }

    /// <summary>
    /// GetOrAdd 的进程内互斥, 避免同一实例内的缓存击穿
    /// </summary>
    protected SemaphoreSlim SyncSemaphore { get; }

    /// <summary>
    /// 默认缓存条目选项
    /// </summary>
    protected DistributedCacheEntryOptions DefaultCacheOptions { get; }

    private readonly DistributedCacheOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">分布式缓存配置</param>
    /// <param name="cache">底层分布式缓存</param>
    public DistributedCache(IOptions<DistributedCacheOptions> options, IDistributedCache cache)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cache);

        _options = options.Value;
        Cache = cache;
        Logger = NullLogger<DistributedCache<TCacheItem, TCacheKey>>.Instance;
        SyncSemaphore = new SemaphoreSlim(1, 1);

        CacheName = DistributedCacheOptions.ResolveCacheName(typeof(TCacheItem));
        DefaultCacheOptions = GetDefaultCacheEntryOptions();
    }

    /// <inheritdoc />
    public virtual TCacheItem? Get(TCacheKey key, bool? hideErrors = null)
    {
        hideErrors ??= _options.HideErrors;

        try
        {
            return ToCacheItem(Cache.Get(NormalizeKey(key)));
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return null;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<TCacheItem?> GetAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default)
    {
        hideErrors ??= _options.HideErrors;

        try
        {
            byte[]? bytes = await Cache.GetAsync(NormalizeKey(key), token).ConfigureAwait(false);
            return ToCacheItem(bytes);
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return null;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public virtual TCacheItem? GetOrAdd(TCacheKey key, Func<TCacheItem> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        TCacheItem? value = Get(key, hideErrors);
        if (value is not null)
        {
            return value;
        }

        SyncSemaphore.Wait();
        try
        {
            value = Get(key, hideErrors);
            if (value is not null)
            {
                return value;
            }

            value = factory();
            Set(key, value, optionsFactory?.Invoke(), hideErrors);
            return value;
        }
        finally
        {
            SyncSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public virtual async Task<TCacheItem?> GetOrAddAsync(TCacheKey key, Func<Task<TCacheItem>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null, CancellationToken token = default)
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
            await SetAsync(key, value, optionsFactory?.Invoke(), hideErrors, token).ConfigureAwait(false);
            return value;
        }
        finally
        {
            SyncSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public virtual void Set(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null)
    {
        hideErrors ??= _options.HideErrors;

        try
        {
            Cache.Set(NormalizeKey(key), JsonSerializer.SerializeToUtf8Bytes(value, DistributedCacheOptions.SerializerOptions), options ?? DefaultCacheOptions);
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task SetAsync(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null, CancellationToken token = default)
    {
        hideErrors ??= _options.HideErrors;

        try
        {
            await Cache.SetAsync(NormalizeKey(key), JsonSerializer.SerializeToUtf8Bytes(value, DistributedCacheOptions.SerializerOptions), options ?? DefaultCacheOptions, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public virtual void Remove(TCacheKey key, bool? hideErrors = null)
    {
        hideErrors ??= _options.HideErrors;

        try
        {
            Cache.Remove(NormalizeKey(key));
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task RemoveAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default)
    {
        hideErrors ??= _options.HideErrors;

        try
        {
            await Cache.RemoveAsync(NormalizeKey(key), token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return;
            }

            throw;
        }
    }

    /// <summary>
    /// 规范化缓存键: 生成 c:{缓存名},k:{前缀}{业务键} 形式的存储键
    /// </summary>
    /// <param name="key">业务键</param>
    /// <returns>最终存储键</returns>
    protected virtual string NormalizeKey(TCacheKey key) => "c:" + CacheName + ",k:" + _options.KeyPrefix + key;

    /// <summary>
    /// 取默认缓存条目选项: 先按缓存名匹配配置器, 再退回全局默认
    /// </summary>
    /// <returns>条目选项</returns>
    protected virtual DistributedCacheEntryOptions GetDefaultCacheEntryOptions()
    {
        foreach (Func<string, DistributedCacheEntryOptions?> configure in _options.CacheConfigurators)
        {
            DistributedCacheEntryOptions? options = configure(CacheName);
            if (options is not null)
            {
                return options;
            }
        }

        return _options.GlobalCacheEntryOptions;
    }

    /// <summary>
    /// 异常处置: 记录日志(缓存故障不应影响主流程)
    /// </summary>
    /// <param name="exception">异常</param>
    protected virtual void HandleException(Exception exception) => Logger.LogError(exception, "分布式缓存操作失败, 缓存名: {CacheName}", CacheName);

    private TCacheItem? ToCacheItem(byte[]? bytes) => bytes is null ? null : JsonSerializer.Deserialize<TCacheItem>(bytes, DistributedCacheOptions.SerializerOptions);
}
