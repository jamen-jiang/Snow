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
/// <para>第一个参数是缓存区名, 与 Spring 的 <c>@CacheEvict(value, key = "#id")</c> 同位置, 必填(失效方法通常返回
/// <see cref="Task"/> / <see cref="void"/>, 没有返回类型可推导)。缓存键是具名表达式 <see cref="Key"/>。</para>
/// <example>
/// <code>
/// [CacheEvict("Acme.Company", AllEntries = true)]
/// public Task UpdateAsync(long id, CompanyInput input) { ... }
///
/// [CacheEvict("Acme.Company", Key = "id")]
/// public Task TouchAsync(long id) { ... }
///
/// [CacheEvict("Acme.Company", Key = "input.Id", Condition = "input.NeedRefresh")]
/// public Task RefreshAsync(CompanyInput input) { ... }
/// </code>
/// </example>
/// <para><see cref="AllEntries"/> 为 true 时清空整个缓存区(代际自增), 此时不需要 <see cref="Key"/>; 为 false 时必须提供。</para>
/// <para><see cref="Key"/> / <see cref="Condition"/> 按 C# 表达式交给 <see cref="ExpressionEngine"/> 求值。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class CacheEvictAttribute : InterceptorAttribute
{
    /// <summary>
    /// 构造器: 给缓存区名
    /// <example>
    /// <code>
    /// [CacheEvict("Acme.Company", AllEntries = true)]
    /// [CacheEvict("Acme.Company", Key = "id")]
    /// </code>
    /// </example>
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
