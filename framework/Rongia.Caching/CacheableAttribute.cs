using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Rongia.Aop;
using Rongia.Expressions;

namespace Rongia.Caching;

/// <summary>
/// 方法级缓存注解(对应 Spring 的 @Cacheable): 命中直接返回, 未命中执行目标方法并回填缓存。
/// <example>
/// <code>
/// [Cacheable("companies", Key = "#id", ExpirationSeconds = 300, Unless = "#result == null")]
/// public async Task&lt;CompanyDto?&gt; GetAsync(long id) { ... }
///
/// [Cacheable("companies", Key = "'page:' + #page + ':' + #size", Condition = "#size &lt;= 100")]
/// public async Task&lt;List&lt;CompanyDto&gt;&gt; GetPageAsync(int page, int size) { ... }
/// </code>
/// </example>
/// <para>返回 <see cref="Task"/> / <see cref="System.Threading.Tasks.ValueTask"/> 且无结果的方法不缓存; 空结果不写缓存(与 Spring 默认一致)。</para>
/// <para><see cref="Key"/> / <see cref="Condition"/> / <see cref="Unless"/> 直接按 SpEL 表达式交给 <see cref="SpelEngine"/> 求值(可用 #参数名 / #p0 / #a0 / #root.methodName / #root.args)。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CacheableAttribute : InterceptorAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    public CacheableAttribute(string cacheName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheName);
        CacheName = cacheName;
    }

    /// <summary>
    /// 缓存名
    /// </summary>
    public string CacheName { get; }

    /// <summary>
    /// 键(SpEL 表达式, 如 "#id" / "'page:' + #page"); 为空时使用「声明类型全名.方法名(参数...)」
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// 缓存条件(SpEL 表达式), 在方法执行前求值, 为 false 时不读写缓存而直接执行方法
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// 排除条件(SpEL 表达式), 在方法执行后求值, 可使用 #result, 为 true 时不写缓存
    /// </summary>
    public string? Unless { get; set; }

    /// <summary>
    /// 过期秒数, 0 表示沿用缓存默认策略
    /// </summary>
    public int ExpirationSeconds { get; set; }

    /// <inheritdoc />
    public override async Task InvokeAsync(AopMethodInvocation invocation)
    {
        Type? payloadType = ResolvePayloadType(invocation.Method.ReturnType);
        if (payloadType is null)
        {
            await invocation.ProceedAsync().ConfigureAwait(false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(Condition) && !SpelEngine.EvaluateBoolean(Condition, CreateContext(invocation, null, false)))
        {
            await invocation.ProceedAsync().ConfigureAwait(false);
            return;
        }

        NamedDistributedCache cache = invocation.ServiceProvider.GetRequiredService<DistributedCacheManager>().GetCache(CacheName);
        string key = ResolveKey(invocation);

        object? cached = await cache.GetAsync(key, payloadType).ConfigureAwait(false);
        if (cached is not null)
        {
            invocation.ReturnValue = cached;
            return;
        }

        await invocation.ProceedAsync().ConfigureAwait(false);

        object? value = invocation.ReturnValue;
        if (value is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(Unless) && SpelEngine.EvaluateBoolean(Unless, CreateContext(invocation, value, true)))
        {
            return;
        }

        DistributedCacheEntryOptions? options = ExpirationSeconds > 0
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ExpirationSeconds) }
            : null;

        await cache.SetAsync(key, value, payloadType, options).ConfigureAwait(false);
    }

    private string ResolveKey(AopMethodInvocation invocation)
    {
        if (!string.IsNullOrWhiteSpace(Key))
        {
            return SpelEngine.EvaluateString(Key, CreateContext(invocation, null, false));
        }

        StringBuilder builder = new();
        builder.Append(invocation.Method.DeclaringType?.FullName ?? "unknown").Append('.').Append(invocation.Method.Name).Append('(');

        for (int index = 0; index < invocation.Arguments.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(invocation.Arguments[index]);
        }

        return builder.Append(')').ToString();
    }

    private static ExpressionContext CreateContext(AopMethodInvocation invocation, object? result, bool hasResult)
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

        if (hasResult)
        {
            context.SetVariable("result", result);
        }

        return context;
    }

    private static Type? ResolvePayloadType(Type returnType)
    {
        if (returnType == typeof(void) || returnType == typeof(Task) || returnType == typeof(System.Threading.Tasks.ValueTask))
        {
            return null;
        }

        if (returnType.IsGenericType)
        {
            Type definition = returnType.GetGenericTypeDefinition();

            if (definition == typeof(Task<>) || definition == typeof(System.Threading.Tasks.ValueTask<>))
            {
                return returnType.GetGenericArguments()[0];
            }
        }

        return returnType;
    }
}
