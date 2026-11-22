namespace Snow.ObjectMapping.Mapster;

/// <summary>
/// 收集宿主在 <c>AddApplication</c> 之前追加的 Mapster 选项配置动作
/// </summary>
public sealed class SnowObjectMappingMapsterOptionsSetup
{
    private readonly List<Action<SnowObjectMappingMapsterOptions>> _actions = [];

    /// <summary>
    /// 追加一个配置动作
    /// </summary>
    /// <param name="action">配置动作</param>
    public void Add(Action<SnowObjectMappingMapsterOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
    }

    /// <summary>
    /// 依次应用全部配置动作
    /// </summary>
    /// <param name="options">目标选项对象</param>
    public void Apply(SnowObjectMappingMapsterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (Action<SnowObjectMappingMapsterOptions> action in _actions)
        {
            action(options);
        }
    }
}
