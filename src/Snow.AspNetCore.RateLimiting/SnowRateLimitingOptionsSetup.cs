using Microsoft.AspNetCore.RateLimiting;

namespace Snow.AspNetCore.RateLimiting;

/// <summary>
/// 收集宿主在 <c>AddApplication</c> 之前追加的限流配置动作
/// <para>动作收到的就是微软自带的 <see cref="RateLimiterOptions"/>, 不做任何包装。</para>
/// </summary>
public sealed class SnowRateLimitingOptionsSetup
{
    private readonly List<Action<RateLimiterOptions>> _actions = [];

    /// <summary>
    /// 追加一个配置动作
    /// </summary>
    /// <param name="action">配置动作</param>
    public void Add(Action<RateLimiterOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
    }

    /// <summary>
    /// 依次应用全部配置动作
    /// </summary>
    /// <param name="options">目标选项对象</param>
    public void Apply(RateLimiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (Action<RateLimiterOptions> action in _actions)
        {
            action(options);
        }
    }
}
