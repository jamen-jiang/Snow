using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.Aop;
using Snow.Expressions;
using BclDistributedCache = Microsoft.Extensions.Caching.Distributed.IDistributedCache;

namespace Snow.Caching;

/// <summary>
/// 方法级缓存注解(对应 Spring 的 @Cacheable): 命中直接返回, 未命中执行目标方法并回填缓存。
/// <para>缓存区由方法返回类型决定: 返回 <c>Task&lt;CompanyCacheItem?&gt;</c> 就落到
/// <see cref="CacheNameAttribute.GetCacheName"/> 推导出的缓存名下, 无需在注解里重复写缓存名。</para>
/// <example>
/// <code>
/// [Cacheable("id", ExpirationSeconds = 300, Unless = "result == null")]
/// public async Task&lt;CompanyCacheItem?&gt; GetAsync(long id) { ... }
///
/// [Cacheable(Key = "\"page:\" + page + \":\" + size", Condition = "size &lt;= 100")]
/// public async Task&lt;List&lt;CompanyCacheItem&gt;&gt; GetPageAsync(int page, int size) { ... }
/// </code>
/// </example>
/// <para>返回 <see cref="Task"/> / <see cref="System.Threading.Tasks.ValueTask"/> 且无结果的方法不缓存; 空结果不写缓存(与 Spring 默认一致)。</para>
/// <para><see cref="Key"/> / <see cref="Condition"/> / <see cref="Unless"/> 按 C# 表达式交给 <see cref="ExpressionEngine"/> 求值(可用参数名 / p0 / a0 / root.methodName / root.args)。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CacheableAttribute : InterceptorAttribute
{
    /// <summary>
    /// 构造器: 可只写 key, 也可只写 <see cref="Condition"/> 等具名属性
    /// </summary>
    /// <param name="key">键表达式</param>
    public CacheableAttribute(string? key = null)
    {
        Key = key;
    }

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

    /// <inheritdoc />
    public override async Task InvokeAsync(AopMethodInvocation invocation)
    {
        Type? payloadType = ResolvePayloadType(invocation.Method.ReturnType);
        if (payloadType is null)
        {
            await invocation.ProceedAsync().ConfigureAwait(false);
            return;
        }

        if (
            !string.IsNullOrWhiteSpace(Condition)
            && !ExpressionEngine.EvaluateBoolean(
                Condition,
                ExpressionContextFactory.Create(invocation)
            )
        )
        {
            await invocation.ProceedAsync().ConfigureAwait(false);
            return;
        }

        BclDistributedCache cache =
            invocation.ServiceProvider.GetRequiredService<BclDistributedCache>();
        DistributedCacheOptions options = invocation
            .ServiceProvider.GetRequiredService<IOptions<DistributedCacheOptions>>()
            .Value;
        ILogger? logger = invocation
            .ServiceProvider.GetService<ILoggerFactory>()
            ?.CreateLogger<CacheableAttribute>();
        string cacheName = CacheNameAttribute.GetCacheName(payloadType);
        string key = ResolveKey(invocation);

        object? cached = await GetAsync(cache, options, logger, cacheName, payloadType, key)
            .ConfigureAwait(false);
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

        if (
            !string.IsNullOrWhiteSpace(Unless)
            && ExpressionEngine.EvaluateBoolean(
                Unless,
                ExpressionContextFactory.Create(invocation, value, true)
            )
        )
        {
            return;
        }

        DistributedCacheEntryOptions? entryOptions =
            ExpirationSeconds > 0
                ? new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ExpirationSeconds),
                }
                : null;

        await SetAsync(cache, options, logger, cacheName, key, value, entryOptions)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 读缓存: 键格式与异常处置统一走 <see cref="DistributedCacheKey"/>
    /// </summary>
    private static async Task<object?> GetAsync(
        BclDistributedCache cache,
        DistributedCacheOptions options,
        ILogger? logger,
        string cacheName,
        Type payloadType,
        string key
    )
    {
        try
        {
            string version = await DistributedCacheKey
                .ReadVersionAsync(cache, cacheName, CancellationToken.None)
                .ConfigureAwait(false);
            byte[]? bytes = await cache
                .GetAsync(DistributedCacheKey.DataKey(cacheName, options.KeyPrefix, version, key))
                .ConfigureAwait(false);

            return bytes is null ? null : DistributedCacheKey.Deserialize(bytes, payloadType);
        }
        catch (Exception exception)
            when (DistributedCacheKey.HandleException(exception, null, options, logger, cacheName))
        {
            return null;
        }
    }

    /// <summary>
    /// 写缓存: 过期策略与门面一致(先问按缓存名定制的配置器)
    /// </summary>
    private static async Task SetAsync(
        BclDistributedCache cache,
        DistributedCacheOptions options,
        ILogger? logger,
        string cacheName,
        string key,
        object value,
        DistributedCacheEntryOptions? entryOptions
    )
    {
        try
        {
            string version = await DistributedCacheKey
                .ReadVersionAsync(cache, cacheName, CancellationToken.None)
                .ConfigureAwait(false);
            byte[] payload = DistributedCacheKey.Serialize(value);

            await cache
                .SetAsync(
                    DistributedCacheKey.DataKey(cacheName, options.KeyPrefix, version, key),
                    payload,
                    entryOptions
                        ?? DistributedCacheKey.GetDefaultCacheEntryOptions(options, cacheName)
                )
                .ConfigureAwait(false);
        }
        catch (Exception exception)
            when (DistributedCacheKey.HandleException(exception, null, options, logger, cacheName))
        { }
    }

    private string ResolveKey(AopMethodInvocation invocation)
    {
        if (!string.IsNullOrWhiteSpace(Key))
        {
            return ExpressionEngine.EvaluateString(
                Key,
                ExpressionContextFactory.Create(invocation)
            );
        }

        StringBuilder builder = new();
        builder
            .Append(invocation.Method.DeclaringType?.FullName ?? "unknown")
            .Append('.')
            .Append(invocation.Method.Name)
            .Append('(');

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

    private static Type? ResolvePayloadType(Type returnType)
    {
        if (
            returnType == typeof(void)
            || returnType == typeof(Task)
            || returnType == typeof(System.Threading.Tasks.ValueTask)
        )
        {
            return null;
        }

        if (returnType.IsGenericType)
        {
            Type definition = returnType.GetGenericTypeDefinition();

            if (
                definition == typeof(Task<>)
                || definition == typeof(System.Threading.Tasks.ValueTask<>)
            )
            {
                return returnType.GetGenericArguments()[0];
            }
        }

        return returnType;
    }
}
