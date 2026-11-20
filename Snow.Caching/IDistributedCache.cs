using Microsoft.Extensions.Caching.Distributed;

namespace Snow.Caching;

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
    where TCacheKey : notnull
{
    /// <summary>
    /// 读取单个缓存项, 不存在返回 null
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <returns>缓存项或 null</returns>
    TCacheItem? Get(TCacheKey key, bool? hideErrors = null);

    /// <summary>
    /// 批量读取, 返回与入参一一对应的键值对, 未命中的项值为 null
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <returns>与入参顺序一致的键值对数组</returns>
    KeyValuePair<TCacheKey, TCacheItem?>[] GetMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null);

    /// <summary>
    /// 批量读取, 返回与入参一一对应的键值对, 未命中的项值为 null
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>与入参顺序一致的键值对数组</returns>
    Task<KeyValuePair<TCacheKey, TCacheItem?>[]> GetManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, CancellationToken token = default);

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
    /// 批量读取, 未命中的键交给工厂一次性补齐
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <param name="factory">入参为未命中的键集合, 返回这些键的值</param>
    /// <param name="optionsFactory">未命中写入时的条目选项工厂</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <returns>与入参顺序一致的键值对数组</returns>
    KeyValuePair<TCacheKey, TCacheItem?>[] GetOrAddMany(IEnumerable<TCacheKey> keys, Func<IEnumerable<TCacheKey>, List<KeyValuePair<TCacheKey, TCacheItem>>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null);

    /// <summary>
    /// 批量读取, 未命中的键交给工厂一次性补齐
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <param name="factory">入参为未命中的键集合, 返回这些键的值</param>
    /// <param name="optionsFactory">未命中写入时的条目选项工厂</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>与入参顺序一致的键值对数组</returns>
    Task<KeyValuePair<TCacheKey, TCacheItem?>[]> GetOrAddManyAsync(IEnumerable<TCacheKey> keys, Func<IEnumerable<TCacheKey>, Task<List<KeyValuePair<TCacheKey, TCacheItem>>>> factory, Func<DistributedCacheEntryOptions>? optionsFactory = null, bool? hideErrors = null, CancellationToken token = default);

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
    /// <returns>表示写入过程的任务</returns>
    Task SetAsync(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions? options = null, bool? hideErrors = null, CancellationToken token = default);

    /// <summary>
    /// 批量写入
    /// </summary>
    /// <param name="items">键值对集合</param>
    /// <param name="options">条目选项; 为 null 时取默认策略</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    void SetMany(IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items, DistributedCacheEntryOptions? options = null, bool? hideErrors = null);

    /// <summary>
    /// 批量写入
    /// </summary>
    /// <param name="items">键值对集合</param>
    /// <param name="options">条目选项; 为 null 时取默认策略</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示写入过程的任务</returns>
    Task SetManyAsync(IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items, DistributedCacheEntryOptions? options = null, bool? hideErrors = null, CancellationToken token = default);

    /// <summary>
    /// 续期缓存项(滑动过期场景用, 不读取值)
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    void Refresh(TCacheKey key, bool? hideErrors = null);

    /// <summary>
    /// 续期缓存项(滑动过期场景用, 不读取值)
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示续期过程的任务</returns>
    Task RefreshAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default);

    /// <summary>
    /// 批量续期(滑动过期场景用, 不读取值)
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    void RefreshMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null);

    /// <summary>
    /// 批量续期(滑动过期场景用, 不读取值)
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示续期过程的任务</returns>
    Task RefreshManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, CancellationToken token = default);

    /// <summary>
    /// 移除单个缓存键
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    void Remove(TCacheKey key, bool? hideErrors = null);

    /// <summary>
    /// 异步移除单个缓存键
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示移除过程的任务</returns>
    Task RemoveAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default);

    /// <summary>
    /// 批量移除
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    void RemoveMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null);

    /// <summary>
    /// 批量移除
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示移除过程的任务</returns>
    Task RemoveManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, CancellationToken token = default);

    /// <summary>
    /// 清空该缓存名下的全部条目(代际自增, O(1))
    /// </summary>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    void Clear(bool? hideErrors = null);

    /// <summary>
    /// 清空该缓存名下的全部条目(代际自增, O(1))
    /// </summary>
    /// <param name="hideErrors">是否吞掉缓存异常; 为 null 时取全局配置</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示清理过程的任务</returns>
    Task ClearAsync(bool? hideErrors = null, CancellationToken token = default);
}