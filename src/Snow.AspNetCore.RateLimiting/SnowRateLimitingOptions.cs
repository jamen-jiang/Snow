using Microsoft.AspNetCore.RateLimiting;

namespace Snow.AspNetCore.RateLimiting;

/// <summary>
/// 限流选项(对齐 <c>SnowDbContextOptions</c> 的聚合写法):
/// <para>宿主通过 <see cref="Configure"/> 追加配置动作, 模块统一在 <see cref="Apply"/> 里执行。</para>
/// </summary>
public sealed class SnowRateLimitingOptions
{
    internal List<Action<RateLimiterOptions>> ConfigureActions { get; } = [];

    /// <summary>
    /// 追加一个限流配置动作; 可多次调用, 按登记顺序执行
    /// </summary>
    /// <param name="action">配置动作, 收到的就是微软原生的 <see cref="RateLimiterOptions"/></param>
    public void Configure(Action<RateLimiterOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        ConfigureActions.Add(action);
    }

    /// <summary>
    /// 依次执行已登记的配置动作
    /// </summary>
    /// <param name="options">微软原生的限流选项</param>
    internal void Apply(RateLimiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (Action<RateLimiterOptions> configureAction in ConfigureActions)
        {
            configureAction(options);
        }
    }
}
