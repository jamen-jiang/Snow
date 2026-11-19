using Microsoft.Extensions.DependencyInjection;
using Rongia.Aop;
using Rongia.Expressions;

namespace Rongia.Caching;

/// <summary>
/// 方法级缓存失效注解(对应 Spring 的 @CacheEvict): 默认在目标方法成功执行后清理, 抛异常则不清理。
/// <example>
/// <code>
/// [CacheEvict("companies", AllEntries = true)]
/// public Task UpdateAsync(long id, CompanyInput input) { ... }
///
/// [CacheEvict("companies", Key = "#id")]
/// public Task TouchAsync(long id) { ... }
///
/// [CacheEvict("companies", Key = "#input.Id", Condition = "#input.NeedRefresh")]
/// public Task RefreshAsync(CompanyInput input) { ... }
/// </code>
/// </example>
/// <para><see cref="AllEntries"/> 为 true 时清空整个缓存名(代际自增); 为 false 时必须提供 <see cref="Key"/>。</para>
/// <para><see cref="Key"/> / <see cref="Condition"/> 直接按 SpEL 表达式交给 <see cref="SpelEngine"/> 求值。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class CacheEvictAttribute : InterceptorAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    public CacheEvictAttribute(string cacheName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheName);
        CacheName = cacheName;
    }

    /// <summary>
    /// 缓存名
    /// </summary>
    public string CacheName { get; }

    /// <summary>
    /// 是否清空该缓存名下的全部条目
    /// </summary>
    public bool AllEntries { get; set; }

    /// <summary>
    /// 键(SpEL 表达式); 仅 <see cref="AllEntries"/> 为 false 时使用
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// 清理条件(SpEL 表达式), 在清理前求值, 为 false 时跳过本次清理
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// 是否在目标方法执行前清理
    /// </summary>
    public bool BeforeInvocation { get; set; }

    /// <inheritdoc />
    public override async Task InvokeAsync(AopMethodInvocation invocation)
    {
        NamedDistributedCache cache = invocation.ServiceProvider.GetRequiredService<DistributedCacheManager>().GetCache(CacheName);

        if (BeforeInvocation)
        {
            await EvictAsync(cache, invocation).ConfigureAwait(false);
        }

        await invocation.ProceedAsync().ConfigureAwait(false);

        if (!BeforeInvocation)
        {
            await EvictAsync(cache, invocation).ConfigureAwait(false);
        }
    }

    private async Task EvictAsync(NamedDistributedCache cache, AopMethodInvocation invocation)
    {
        if (!string.IsNullOrWhiteSpace(Condition) && !SpelEngine.EvaluateBoolean(Condition, CreateContext(invocation)))
        {
            return;
        }

        if (AllEntries)
        {
            await cache.ClearAsync().ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException("[" + nameof(CacheEvictAttribute) + "] 在 " + nameof(AllEntries) + " = false 时必须提供 " + nameof(Key) + "。");
        }

        await cache.RemoveAsync(SpelEngine.EvaluateString(Key, CreateContext(invocation))).ConfigureAwait(false);
    }

    private static ExpressionContext CreateContext(AopMethodInvocation invocation)
    {
        ExpressionContext context = new(new ExpressionRoot(invocation.Method.Name, invocation.Method, invocation.Target, invocation.Arguments));

        for (int index = 0; index < invocation.Arguments.Length; index++)
        {
            context.SetVariable("p" + index, invocation.Arguments[index]);
            context.SetVariable("a" + index, invocation.Arguments[index]);
        }

        foreach (KeyValuePair<string, object?> pair in invocation.ArgumentsDictionary)
        {
            context.SetVariable(pair.Key, pair.Value);
        }

        return context;
    }
}
