namespace Snow.Caching;

/// <summary>
/// 方法级缓存标记(对应 Spring 的 @Cacheable): 命中直接返回, 未命中执行目标方法并回填缓存。
/// <para>第一个参数是缓存区名, 与 Spring 的 <c>@Cacheable(value, key = "#id")</c> 同位置, 必填。缓存键是具名表达式
/// <see cref="Key"/>, 不写时用「声明类型全名.方法名(参数...)」。</para>
/// <example>
/// <code>
/// [Cacheable("Acme.User")]
/// public async Task&lt;UserCacheItem?&gt; GetAsync(long id) { ... }
///
/// [Cacheable("Acme.User", Key = "id", ExpirationSeconds = 300, Unless = "result == null")]
/// public async Task&lt;UserCacheItem?&gt; GetByIdAsync(long id) { ... }
/// </code>
/// </example>
/// <para>本特性只是元数据, 干活的是 <see cref="CacheableInterceptor"/>, 由
/// <see cref="SnowCachingInterceptorRegistrar"/> 按它的存在挂上去。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CacheableAttribute : Attribute
{
    /// <summary>
    /// 构造器: 给缓存区名
    /// </summary>
    /// <param name="cacheName">缓存区名, 按字面使用</param>
    public CacheableAttribute(string cacheName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheName);

        CacheName = cacheName;
    }

    /// <summary>
    /// 缓存区名
    /// </summary>
    public string CacheName { get; }

    /// <summary>
    /// 键(表达式, 如 "id" / "\"page:\" + page"); 为空时使用「声明类型全名.方法名(参数...)」
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// 缓存条件(表达式), 在方法执行前求值, 为 false 时不读写缓存而直接执行方法
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// 排除条件(表达式), 在方法执行后求值, 可使用 result, 为 true 时不写缓存
    /// </summary>
    public string? Unless { get; set; }

    /// <summary>
    /// 过期秒数, 0 表示沿用缓存默认策略
    /// </summary>
    public int ExpirationSeconds { get; set; }
}
