namespace Snow.OpenApi;

/// <summary>
/// 收集宿主在 <c>AddApplication</c> 之前追加的 OpenAPI 选项配置动作
/// </summary>
public sealed class SnowSwaggerOptionsSetup
{
    private readonly List<Action<SnowSwaggerOptions>> _actions = [];

    /// <summary>
    /// 追加一个配置动作
    /// </summary>
    /// <param name="action">配置动作</param>
    public void Add(Action<SnowSwaggerOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
    }

    /// <summary>
    /// 依次应用全部配置动作
    /// </summary>
    /// <param name="options">目标选项对象</param>
    public void Apply(SnowSwaggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (Action<SnowSwaggerOptions> action in _actions)
        {
            action(options);
        }
    }
}
