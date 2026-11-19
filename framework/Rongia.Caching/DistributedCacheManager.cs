using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Rongia.Caching;

/// <summary>
/// 缓存管理器: 按名字取 <see cref="NamedDistributedCache"/> 实例, 同名复用同一个实例
/// </summary>
public sealed class DistributedCacheManager
{
    private readonly ConcurrentDictionary<string, NamedDistributedCache> _caches = new(StringComparer.Ordinal);
    private readonly IOptions<DistributedCacheOptions> _options;
    private readonly IDistributedCache _cache;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">分布式缓存配置</param>
    /// <param name="cache">底层分布式缓存</param>
    public DistributedCacheManager(IOptions<DistributedCacheOptions> options, IDistributedCache cache)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cache);

        _options = options;
        _cache = cache;
    }

    /// <summary>
    /// 取指定名字的缓存, 同名多次调用返回同一实例
    /// </summary>
    /// <param name="name">缓存名</param>
    /// <returns>缓存实例</returns>
    public NamedDistributedCache GetCache(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _caches.GetOrAdd(name, key => new NamedDistributedCache(key, _options.Value, _cache));
    }
}
