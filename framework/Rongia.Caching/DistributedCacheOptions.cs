using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Rongia.Caching;

/// <summary>
/// 分布式缓存全局配置: 键前缀、默认过期策略、异常隐藏开关与按缓存名定制的条目选项
/// </summary>
public sealed class DistributedCacheOptions
{
    /// <summary>
    /// 配置节名
    /// </summary>
    public const string SectionName = "DistributedCache";

    /// <summary>
    /// 缓存值序列化选项(Web 默认: 驼峰命名)
    /// </summary>
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 是否吞掉缓存异常(默认吞掉, 缓存故障不应拖垮业务)
    /// </summary>
    public bool HideErrors { get; set; } = true;

    /// <summary>
    /// 缓存键统一前缀, 便于多应用共用同一 Redis 实例时隔离
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// 全局默认缓存条目选项
    /// </summary>
    public DistributedCacheEntryOptions GlobalCacheEntryOptions { get; set; } = new DistributedCacheEntryOptions();

    /// <summary>
    /// 按缓存名定制条目选项的配置器集合(入参为缓存名, 返回 null 表示不匹配)
    /// </summary>
    public List<Func<string, DistributedCacheEntryOptions?>> CacheConfigurators { get; } = [];

    /// <summary>
    /// 为指定缓存项类型定制条目选项
    /// </summary>
    /// <typeparam name="TCacheItem">缓存项类型</typeparam>
    /// <param name="options">条目选项</param>
    public void ConfigureCache<TCacheItem>(DistributedCacheEntryOptions? options) => ConfigureCache(typeof(TCacheItem), options);

    /// <summary>
    /// 为指定缓存项类型定制条目选项
    /// </summary>
    /// <param name="cacheItemType">缓存项类型</param>
    /// <param name="options">条目选项</param>
    public void ConfigureCache(Type cacheItemType, DistributedCacheEntryOptions? options) => ConfigureCache(ResolveCacheName(cacheItemType), options);

    /// <summary>
    /// 为指定缓存名定制条目选项
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="options">条目选项</param>
    public void ConfigureCache(string cacheName, DistributedCacheEntryOptions? options)
        => CacheConfigurators.Add(name => string.Equals(name, cacheName, StringComparison.Ordinal) ? options : null);

    /// <summary>
    /// 取缓存项类型对应的逻辑缓存名: 类型全名去掉 CacheItem 后缀
    /// </summary>
    /// <param name="cacheItemType">缓存项类型</param>
    /// <returns>缓存名</returns>
    internal static string ResolveCacheName(Type cacheItemType)
    {
        ArgumentNullException.ThrowIfNull(cacheItemType);

        string fullName = cacheItemType.FullName ?? cacheItemType.Name;
        const string suffix = "CacheItem";

        return fullName.EndsWith(suffix, StringComparison.Ordinal) ? fullName[..^suffix.Length] : fullName;
    }
}
