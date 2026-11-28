using System.Reflection;
using Snow.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.DynamicProxy;
using Snow.Expressions;
using BclDistributedCache = Microsoft.Extensions.Caching.Distributed.IDistributedCache;

namespace Snow.Caching;

/// <summary>
/// 缓存失效拦截器: 按 <see cref="CacheEvictAttribute"/> 在方法前/后清理缓存
/// </summary>
[TransientService]
public sealed class CacheEvictInterceptor : SnowInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public CacheEvictInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public override async Task InterceptAsync(ISnowMethodInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        CacheEvictAttribute[] attributes = invocation.Method.GetCustomAttributes<CacheEvictAttribute>(true).ToArray();
        if (attributes.Length == 0)
        {
            await invocation.ProceedAsync().ConfigureAwait(false);
            return;
        }

        BclDistributedCache cache = _serviceProvider.GetRequiredService<BclDistributedCache>();
        DistributedCacheOptions options = _serviceProvider.GetRequiredService<IOptions<DistributedCacheOptions>>().Value;
        ILogger? logger = _serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<CacheEvictInterceptor>();

        foreach (CacheEvictAttribute attribute in attributes)
        {
            if (attribute.BeforeInvocation)
            {
                await EvictAsync(cache, options, logger, attribute, invocation).ConfigureAwait(false);
            }
        }

        await invocation.ProceedAsync().ConfigureAwait(false);

        foreach (CacheEvictAttribute attribute in attributes)
        {
            if (!attribute.BeforeInvocation)
            {
                await EvictAsync(cache, options, logger, attribute, invocation).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// 按注解清理缓存: 配了 <see cref="CacheEvictAttribute.Condition"/> 且不成立时跳过;
    /// <see cref="CacheEvictAttribute.AllEntries"/> 时整段失效(版本号自增), 否则删单个键
    /// </summary>
    /// <param name="cache">分布式缓存</param>
    /// <param name="options">缓存选项</param>
    /// <param name="logger">日志; 可为 <c>null</c></param>
    /// <param name="attribute">失效注解</param>
    /// <param name="invocation">方法调用</param>
    /// <returns>表示清理过程的任务</returns>
    private static async Task EvictAsync(
        BclDistributedCache cache,
        DistributedCacheOptions options,
        ILogger? logger,
        CacheEvictAttribute attribute,
        ISnowMethodInvocation invocation)
    {
        if (!string.IsNullOrWhiteSpace(attribute.Condition) && !ExpressionEngine.EvaluateBoolean(attribute.Condition, ExpressionContextFactory.Create(invocation)))
        {
            return;
        }

        try
        {
            if (attribute.AllEntries)
            {
                await DistributedCacheKey.BumpVersionAsync(cache, attribute.CacheName, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(attribute.Key))
            {
                throw new InvalidOperationException("[" + nameof(CacheEvictAttribute) + "] 在 " + nameof(CacheEvictAttribute.AllEntries) + " = false 时必须提供 " + nameof(CacheEvictAttribute.Key) + "。");
            }

            string version = await DistributedCacheKey.ReadVersionAsync(cache, attribute.CacheName, CancellationToken.None).ConfigureAwait(false);
            string key = ExpressionEngine.EvaluateString(attribute.Key, ExpressionContextFactory.Create(invocation));

            await cache.RemoveAsync(DistributedCacheKey.DataKey(attribute.CacheName, options.KeyPrefix, version, key)).ConfigureAwait(false);
        }
        catch (Exception exception) when (DistributedCacheKey.HandleException(exception, null, options, logger, attribute.CacheName))
        {
        }
    }
}
