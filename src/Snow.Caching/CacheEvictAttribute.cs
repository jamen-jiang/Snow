namespace Snow.Caching;

/// <summary>
/// 方法级缓存失效标记(对应 Spring 的 @CacheEvict): 默认在目标方法成功执行后清理, 抛异常则不清理。
/// <para>第一个参数是缓存区名, 与 Spring 的 <c>@CacheEvict(value, key = "#id")</c> 同位置, 必填。缓存键是具名表达式 <see cref="Key"/>。</para>
/// <example>
/// <code>
/// [CacheEvict("Acme.Company", AllEntries = true)]
/// public Task UpdateAsync(long id, CompanyInput input) { ... }
///
/// [CacheEvict("Acme.Company", Key = "id")]
/// public Task TouchAsync(long id) { ... }
/// </code>
/// </example>
/// <para>本特性只是元数据, 干活的是 <see cref="CacheEvictInterceptor"/>, 由
/// <see cref="SnowCachingInterceptorRegistrar"/> 按它的存在挂上去。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class CacheEvictAttribute : Attribute
{
    /// <summary>
    /// 构造器: 给缓存区名
    /// </summary>
    /// <param name="cacheName">缓存区名, 按字面使用</param>
    public CacheEvictAttribute(string cacheName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheName);

        CacheName = cacheName;
    }

    /// <summary>
    /// 缓存区名
    /// </summary>
    public string CacheName { get; }

    /// <summary>
    /// 是否清空该缓存名下的全部条目
    /// </summary>
    public bool AllEntries { get; set; }

    /// <summary>
    /// 键(表达式); 仅 <see cref="AllEntries"/> 为 false 时使用
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// 清理条件(表达式), 在清理前求值, 为 false 时跳过本次清理
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// 是否在目标方法执行前清理
    /// </summary>
    public bool BeforeInvocation { get; set; }
}
