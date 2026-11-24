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
/// <para>第一个参数是缓存区名, 与 Spring 的 <c>@Cacheable(value, key = "#id")</c> 同位置, 必填。缓存键是具名表达式
/// <see cref="Key"/>, 不写时用「声明类型全名.方法名(参数...)」。</para>
/// <example>
/// <code>
/// [Cacheable("Acme.User")]
/// public async Task&lt;UserCacheItem?&gt; GetAsync(long id) { ... }
///
/// [Cacheable("Acme.User", Key = "id", ExpirationSeconds = 300, Unless = "result == null")]
/// public async Task&lt;UserCacheItem?&gt; GetByIdAsync(long id) { ... }
///
/// [Cacheable("Acme.User", Key = "\"page:\" + page + \":\" + size", Condition = "size &lt;= 100")]
/// public async Task&lt;List&lt;UserCacheItem&gt;&gt; GetPageAsync(int page, int size) { ... }
/// </code>
/// </example>
/// <para>返回 <see cref="Task"/> / <see cref="System.Threading.Tasks.ValueTask"/> 且无结果的方法不缓存; 空结果不写缓存(与 Spring 默认一致)。</para>
/// <para>缓存区名与返回类型解耦: 同一份数据可以由不同方法写进同一个区, 但读回时仍按方法返回类型反序列化,
/// 类型不一致会反序列化失败并当作未命中。</para>
/// <para><see cref="Key"/> / <see cref="Condition"/> / <see cref="Unless"/> 按 C# 表达式交给 <see cref="ExpressionEngine"/> 求值(可用参数名 / p0 / a0 / root.methodName / root.args)。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CacheableAttribute : SnowInterceptorAttribute
{
    /// <summary>
    /// 构造器: 给缓存区名
    /// <example>
    /// <code>
    /// [Cacheable("Acme.User", Key = "id")]
    /// </code>
    /// </example>
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

    /// <inheritdoc />
    public override async Task InvokeAsync(AspectContext context, AspectDelegate next, CancellationToken cancellationToken = default)
    {
        Type? payloadType = ResolvePayloadType(context.Method.ReturnType);
        if (payloadType is null)
        {
            await next(context, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (
            !string.IsNullOrWhiteSpace(Condition)
            && !ExpressionEngine.EvaluateBoolean(
                Condition,
                ExpressionContextFactory.Create(context)
            )
        )
        {
            await next(context, cancellationToken).ConfigureAwait(false);
            return;
        }

        BclDistributedCache cache =
            context.ServiceProvider.GetRequiredService<BclDistributedCache>();
        DistributedCacheOptions options = context
            .ServiceProvider.GetRequiredService<IOptions<DistributedCacheOptions>>()
            .Value;
        ILogger? logger = context
            .ServiceProvider.GetService<ILoggerFactory>()
            ?.CreateLogger<CacheableAttribute>();
        string cacheName = CacheName;
        string key = ResolveKey(context);

        object? cached = await GetAsync(cache, options, logger, cacheName, payloadType, key)
            .ConfigureAwait(false);
        if (cached is not null)
        {
            context.ReturnValue = cached;
            return;
        }

        await next(context, cancellationToken).ConfigureAwait(false);

        object? value = context.ReturnValue;
        if (value is null)
        {
            return;
        }

        if (
            !string.IsNullOrWhiteSpace(Unless)
            && ExpressionEngine.EvaluateBoolean(
                Unless,
                ExpressionContextFactory.Create(context, value, true)
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

    private string ResolveKey(AspectContext context)
    {
        if (!string.IsNullOrWhiteSpace(Key))
        {
            return ExpressionEngine.EvaluateString(
                Key,
                ExpressionContextFactory.Create(context)
            );
        }

        StringBuilder builder = new();
        builder
            .Append(context.Method.DeclaringType?.FullName ?? "unknown")
            .Append('.')
            .Append(context.Method.Name)
            .Append('(');

        for (int index = 0; index < context.Arguments.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(context.Arguments[index]);
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
