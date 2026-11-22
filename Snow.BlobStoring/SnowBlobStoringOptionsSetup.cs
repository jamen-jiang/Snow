namespace Snow.BlobStoring;

/// <summary>
/// 收集宿主在 <c>AddApplication</c> 之前追加的对象存储选项配置动作
/// </summary>
public sealed class SnowBlobStoringOptionsSetup
{
    private readonly List<Action<SnowBlobStoringOptions>> _actions = [];

    /// <summary>
    /// 追加一个配置动作
    /// </summary>
    /// <param name="action">配置动作</param>
    public void Add(Action<SnowBlobStoringOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
    }

    /// <summary>
    /// 依次应用全部配置动作
    /// </summary>
    /// <param name="options">目标选项对象</param>
    public void Apply(SnowBlobStoringOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (Action<SnowBlobStoringOptions> action in _actions)
        {
            action(options);
        }
    }
}
