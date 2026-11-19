using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Rongia.Caching;

/// <summary>
/// 强类型分布式缓存(键为字符串): 缓存项类型即一个逻辑缓存区
/// </summary>
/// <typeparam name="TCacheItem">缓存项类型</typeparam>
public interface IDistributedCache<TCacheItem> : IDistributedCache<TCacheItem, string>
    where TCacheItem : class
{
    /// <summary>
    /// 内部实现(键类型为字符串)
    /// </summary>
    IDistributedCache<TCacheItem, string> InternalCache { get; }
}

/// <summary>
/// 强类型分布式缓存: 键类型可自定义
/// </summary>
/// <typeparam name="TCacheItem">缓存项类型</typeparam>
/// <typeparam name="TCacheKey">缓存键类型</typeparam>
public interface IDistributedCache<TCacheItem, TCacheKey>
    where TCacheItem : class
{
    /// <summary>
    /// 读取单个缓存项, 不存在返回 null
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <returns>缓存项或 null</returns>
    TCacheItem? Get(TCacheKey key, bool? hideErrors = null);

    /// <summary>
    /// 异步读取单个缓存项, 不存在返回 null
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>缓存项或 null</returns>
    Task<TCacheItem?> GetAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default);

    /// <summary>
    /// 读取缓存项, 不存在则用工厂生成并写入
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="factory">未命中时的值工厂</param>
    /// <param name="optionsFactory">未命中写入时的条目选项工厂</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <returns>缓存项</returns>
    TCacheItem? GetOrAdd(TCacheKey key, Func<TCacheItem> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null);

    /// <summary>
    /// 异步读取缓存项, 不存在则用工厂生成并写入
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="factory">未命中时的值工厂</param>
    /// <param name="optionsFactory">未命中写入时的条目选项工厂</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>缓存项</returns>
    Task<TCacheItem?> GetOrAddAsync(TCacheKey key, Func<Task<TCacheItem>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null, CancellationToken token = default);

    /// <summary>
    /// 写入单个缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存项</param>
    /// <param name="options">条目选项; 为 null 时取默认策略</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    void Set(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null);

    /// <summary>
    /// 异步写入单个缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存项</param>
    /// <param name="options">条目选项; 为 null 时取默认策略</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    Task SetAsync(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null, CancellationToken token = default);

    /// <summary>
    /// 删除单个缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    void Remove(TCacheKey key, bool? hideErrors = null);

    /// <summary>
    /// 异步删除单个缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    Task RemoveAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default);
}
