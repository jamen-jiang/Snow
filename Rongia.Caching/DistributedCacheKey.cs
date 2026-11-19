using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using BclDistributedCache = Microsoft.Extensions.Caching.Distributed.IDistributedCache;

namespace Rongia.Caching;

/// <summary>
/// 缓存键与值的唯一实现: 强类型缓存门面与 <see cref="CacheableAttribute"/> / <see cref="CacheEvictAttribute"/> 共用。
/// <para>注解手上只有 <see cref="Type"/>, 注入不进 <see cref="IDistributedCache{TCacheItem}"/>; 键格式若在两边各写一份,
/// 门面改格式后注解会静默全部 miss, 因此集中到这里。</para>
/// </summary>
internal static class DistributedCacheKey
{
    internal const string KeyRoot = "rongia:cache";

    /// <summary>
    /// 代际版本键: 缓存名下所有条目共享一个版本号, 版本 +1 即整区失效
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <returns>版本键</returns>
    internal static string VersionKey(string cacheName) => KeyRoot + ":" + cacheName + ":__version__";

    /// <summary>
    /// 数据键: 缓存名 + 代际 + 全局键前缀 + 业务键
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="keyPrefix">全局键前缀</param>
    /// <param name="version">当前代际</param>
    /// <param name="key">业务键</param>
    /// <returns>底层存储键</returns>
    internal static string DataKey(string cacheName, string keyPrefix, string version, string key)
        => KeyRoot + ":" + cacheName + ":v" + version + ":" + keyPrefix + key;

    /// <summary>
    /// 读当前代际(无记录视为第 1 代)
    /// </summary>
    /// <param name="cache">底层字节缓存</param>
    /// <param name="cacheName">缓存名</param>
    /// <returns>代际</returns>
    internal static string ReadVersion(BclDistributedCache cache, string cacheName)
    {
        string? version = cache.GetString(VersionKey(cacheName));

        return string.IsNullOrEmpty(version) ? "1" : version;
    }

    /// <summary>
    /// 读当前代际(异步)
    /// </summary>
    /// <param name="cache">底层字节缓存</param>
    /// <param name="cacheName">缓存名</param>
    /// <param name="token">取消令牌</param>
    /// <returns>代际</returns>
    internal static async Task<string> ReadVersionAsync(BclDistributedCache cache, string cacheName, CancellationToken token)
    {
        string? version = await cache.GetStringAsync(VersionKey(cacheName), token).ConfigureAwait(false);

        return string.IsNullOrEmpty(version) ? "1" : version;
    }

    /// <summary>
    /// 推进代际, 使该缓存名下的全部条目失效
    /// </summary>
    /// <param name="cache">底层字节缓存</param>
    /// <param name="cacheName">缓存名</param>
    internal static void BumpVersion(BclDistributedCache cache, string cacheName)
    {
        int current = int.TryParse(ReadVersion(cache, cacheName), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 1;

        cache.SetString(VersionKey(cacheName), (current + 1).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 推进代际(异步)
    /// </summary>
    /// <param name="cache">底层字节缓存</param>
    /// <param name="cacheName">缓存名</param>
    /// <param name="token">取消令牌</param>
    /// <returns>表示操作的任务</returns>
    internal static async Task BumpVersionAsync(BclDistributedCache cache, string cacheName, CancellationToken token)
    {
        int current = int.TryParse(await ReadVersionAsync(cache, cacheName, token).ConfigureAwait(false), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 1;

        await cache.SetStringAsync(VersionKey(cacheName), (current + 1).ToString(CultureInfo.InvariantCulture), token).ConfigureAwait(false);
    }

    /// <summary>
    /// 条目选项: 先问按缓存名定制的配置器, 都没命中则用全局默认
    /// </summary>
    /// <param name="options">分布式缓存配置</param>
    /// <param name="cacheName">缓存名</param>
    /// <returns>条目选项</returns>
    internal static Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions GetDefaultCacheEntryOptions(DistributedCacheOptions options, string cacheName)
    {
        foreach (Func<string, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions?> configure in options.CacheConfigurators)
        {
            Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions? configured = configure(cacheName);

            if (configured is not null)
            {
                return configured;
            }
        }

        return options.GlobalCacheEntryOptions;
    }

    /// <summary>
    /// 序列化缓存值
    /// </summary>
    /// <param name="value">缓存值</param>
    /// <returns>UTF-8 字节</returns>
    internal static byte[] Serialize(object value) => JsonSerializer.SerializeToUtf8Bytes(value, DistributedCacheOptions.SerializerOptions);

    /// <summary>
    /// 反序列化缓存值
    /// </summary>
    /// <param name="bytes">UTF-8 字节</param>
    /// <param name="type">目标类型</param>
    /// <returns>缓存值</returns>
    internal static object? Deserialize(byte[] bytes, Type type) => JsonSerializer.Deserialize(bytes, type, DistributedCacheOptions.SerializerOptions);

    /// <summary>
    /// 异常处置: 缓存故障不应影响主流程, 返回 true 表示已吞掉
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="hideErrors">是否吞掉; 为 null 时取全局配置</param>
    /// <param name="options">分布式缓存配置</param>
    /// <param name="logger">日志</param>
    /// <param name="cacheName">缓存名</param>
    /// <returns>是否吞掉该异常</returns>
    internal static bool HandleException(Exception exception, bool? hideErrors, DistributedCacheOptions options, ILogger? logger, string cacheName)
    {
        if (!(hideErrors ?? options.HideErrors))
        {
            return false;
        }

        logger?.LogError(exception, "分布式缓存操作失败, 缓存名: {CacheName}", cacheName);

        return true;
    }
}
