using System.Reflection;
using Snow.DependencyInjection;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.DynamicProxy;
using Snow.Expressions;
using BclDistributedCache = Microsoft.Extensions.Caching.Distributed.IDistributedCache;

namespace Snow.Caching;

/// <summary>
/// 缓存拦截器: 按 <see cref="CacheableAttribute"/> 读缓存、执行目标方法、回填缓存
/// </summary>
[TransientService]
public sealed class CacheableInterceptor : SnowInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public CacheableInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public override async Task InterceptAsync(ISnowMethodInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        CacheableAttribute? attribute = invocation.Method.GetCustomAttribute<CacheableAttribute>(true);
        if (attribute is null)
        {
            await invocation.ProceedAsync().ConfigureAwait(false);
            return;
        }

        Type? payloadType = ResolvePayloadType(invocation.Method.ReturnType);
        if (payloadType is null)
        {
            await invocation.ProceedAsync().ConfigureAwait(false);
            return;
        }

        if (
            !string.IsNullOrWhiteSpace(attribute.Condition)
            && !ExpressionEngine.EvaluateBoolean(
                attribute.Condition,
                ExpressionContextFactory.Create(invocation)
            )
        )
        {
            await invocation.ProceedAsync().ConfigureAwait(false);
            return;
        }

        BclDistributedCache cache = _serviceProvider.GetRequiredService<BclDistributedCache>();
        DistributedCacheOptions options = _serviceProvider.GetRequiredService<IOptions<DistributedCacheOptions>>().Value;
        ILogger? logger = _serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<CacheableInterceptor>();
        string cacheName = attribute.CacheName;
        string key = ResolveKey(attribute, invocation);

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
            !string.IsNullOrWhiteSpace(attribute.Unless)
            && ExpressionEngine.EvaluateBoolean(
                attribute.Unless,
                ExpressionContextFactory.Create(invocation, value, true)
            )
        )
        {
            return;
        }

        DistributedCacheEntryOptions? entryOptions =
            attribute.ExpirationSeconds > 0
                ? new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(attribute.ExpirationSeconds),
                }
                : null;

        await SetAsync(cache, options, logger, cacheName, key, value, entryOptions)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 读缓存: 键格式与异常处置统一走 <see cref="DistributedCacheKey"/>
    /// </summary>
    /// <param name="cache">分布式缓存</param>
    /// <param name="options">缓存选项</param>
    /// <param name="logger">日志; 可为 <c>null</c></param>
    /// <param name="cacheName">缓存名</param>
    /// <param name="payloadType">反序列化的目标类型</param>
    /// <param name="key">缓存键</param>
    /// <returns>命中的缓存值; 未命中返回 <c>null</c></returns>
    private static async Task<object?> GetAsync(
        BclDistributedCache cache,
        DistributedCacheOptions options,
        ILogger? logger,
        string cacheName,
        Type payloadType,
        string key)
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
    /// <param name="cache">分布式缓存</param>
    /// <param name="options">缓存选项</param>
    /// <param name="logger">日志; 可为 <c>null</c></param>
    /// <param name="cacheName">缓存名</param>
    /// <param name="key">缓存键</param>
    /// <param name="value">要写入的值</param>
    /// <param name="entryOptions">过期选项; 为 <c>null</c> 时取默认值</param>
    /// <returns>表示写入过程的任务</returns>
    private static async Task SetAsync(
        BclDistributedCache cache,
        DistributedCacheOptions options,
        ILogger? logger,
        string cacheName,
        string key,
        object value,
        DistributedCacheEntryOptions? entryOptions)
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
                        ?? DistributedCacheKey.GetDefaultCacheEntryOptions(options, cacheName))
                .ConfigureAwait(false);
        }
        catch (Exception exception)
            when (DistributedCacheKey.HandleException(exception, null, options, logger, cacheName))
        { }
    }

    /// <summary>
    /// 计算缓存键: 配了 <see cref="CacheableAttribute.Key"/> 就按表达式求值, 否则用「类型全名.方法名(实参...)」
    /// </summary>
    /// <param name="attribute">缓存注解</param>
    /// <param name="invocation">方法调用</param>
    /// <returns>缓存键</returns>
    private static string ResolveKey(CacheableAttribute attribute, ISnowMethodInvocation invocation)
    {
        if (!string.IsNullOrWhiteSpace(attribute.Key))
        {
            return ExpressionEngine.EvaluateString(
                attribute.Key,
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

    /// <summary>
    /// 取真正要缓存的类型: 剥掉 <see cref="Task{TResult}"/> / <see cref="ValueTask{TResult}"/> 的外壳; 无返回值或返回非泛型 Task 时返回 <c>null</c>
    /// </summary>
    /// <param name="returnType">方法返回类型</param>
    /// <returns>缓存载荷类型; 不该缓存时为 <c>null</c></returns>
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
