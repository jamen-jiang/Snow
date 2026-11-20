using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.Aop;
using Snow.Expressions;
using BclDistributedCache = Microsoft.Extensions.Caching.Distributed.IDistributedCache;

namespace Snow.Caching;

/// <summary>
/// 方法级缓存失效注解(对应 Spring 的 @CacheEvict): 默认在目标方法成功执行后清理, 抛异常则不清理。
/// <para>必须给出缓存项类型: 失效动作往往发生在返回 void 的方法上, 无法像 <see cref="CacheableAttribute"/> 那样从返回类型推导。</para>
/// <example>
/// <code>
/// [CacheEvict(typeof(CompanyCacheItem), AllEntries = true)]
/// public Task UpdateAsync(long id, CompanyInput input) { ... }
///
/// [CacheEvict(typeof(CompanyCacheItem), Key = "id")]
/// public Task TouchAsync(long id) { ... }
///
/// [CacheEvict(typeof(CompanyCacheItem), Key = "input.Id", Condition = "input.NeedRefresh")]
/// public Task RefreshAsync(CompanyInput input) { ... }
/// </code>
/// </example>
/// <para><see cref="AllEntries"/> 为 true 时清空整个缓存区(代际自增); 为 false 时必须提供 <see cref="Key"/>。</para>
/// <para><see cref="Key"/> / <see cref="Condition"/> 按 C# 表达式交给 <see cref="ExpressionEngine"/> 求值。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class CacheEvictAttribute : InterceptorAttribute
{
    /// <summary>
    /// 构造器: 可以只给缓存项类型(配合 <see cref="AllEntries"/>), 也可以位置参数直接给键
    /// <example>
    /// <code>
    /// [CacheEvict(typeof(CompanyCacheItem), AllEntries = true)]
    /// [CacheEvict(typeof(CompanyCacheItem), "id")]
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="cacheItemType">缓存项类型, 决定清理哪个缓存区</param>
    /// <param name="key">键表达式, 与具名属性 <see cref="Key"/> 等价</param>
    public CacheEvictAttribute(Type cacheItemType, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(cacheItemType);

        CacheItemType = cacheItemType;
        CacheName = CacheNameAttribute.GetCacheName(cacheItemType);
        Key = key;
    }

    /// <summary>
    /// 缓存项类型
    /// </summary>
    public Type CacheItemType { get; }

    /// <summary>
    /// 缓存名(由 <see cref="CacheItemType"/> 推导)
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

    /// <inheritdoc />
    public override async Task InvokeAsync(AopMethodInvocation invocation)
    {
        BclDistributedCache cache = invocation.ServiceProvider.GetRequiredService<BclDistributedCache>();
        DistributedCacheOptions options = invocation.ServiceProvider.GetRequiredService<IOptions<DistributedCacheOptions>>().Value;
        ILogger? logger = invocation.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger<CacheEvictAttribute>();

        if (BeforeInvocation)
        {
            await EvictAsync(cache, options, logger, invocation).ConfigureAwait(false);
        }

        await invocation.ProceedAsync().ConfigureAwait(false);

        if (!BeforeInvocation)
        {
            await EvictAsync(cache, options, logger, invocation).ConfigureAwait(false);
        }
    }

    private async Task EvictAsync(BclDistributedCache cache, DistributedCacheOptions options, ILogger? logger, AopMethodInvocation invocation)
    {
        if (!string.IsNullOrWhiteSpace(Condition) && !ExpressionEngine.EvaluateBoolean(Condition, ExpressionContextFactory.Create(invocation)))
        {
            return;
        }

        try
        {
            if (AllEntries)
            {
                await DistributedCacheKey.BumpVersionAsync(cache, CacheName, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(Key))
            {
                throw new InvalidOperationException("[" + nameof(CacheEvictAttribute) + "] 在 " + nameof(AllEntries) + " = false 时必须提供 " + nameof(Key) + "。");
            }

            string version = await DistributedCacheKey.ReadVersionAsync(cache, CacheName, CancellationToken.None).ConfigureAwait(false);
            string key = ExpressionEngine.EvaluateString(Key, ExpressionContextFactory.Create(invocation));

            await cache.RemoveAsync(DistributedCacheKey.DataKey(CacheName, options.KeyPrefix, version, key)).ConfigureAwait(false);
        }
        catch (Exception exception) when (DistributedCacheKey.HandleException(exception, null, options, logger, CacheName))
        {
        }
    }

}
