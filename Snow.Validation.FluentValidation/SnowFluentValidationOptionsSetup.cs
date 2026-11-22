namespace Snow.Validation.FluentValidation;

/// <summary>
/// 收集宿主在 PreConfigureServices 阶段追加的 FluentValidation 选项配置动作
/// </summary>
public sealed class SnowFluentValidationOptionsSetup
{
    private readonly List<Action<SnowFluentValidationOptions>> _actions = [];

    /// <summary>
    /// 追加一个配置动作
    /// </summary>
    /// <param name="action">配置动作</param>
    public void Add(Action<SnowFluentValidationOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
    }

    /// <summary>
    /// 依次应用全部配置动作
    /// </summary>
    /// <param name="options">目标选项对象</param>
    public void Apply(SnowFluentValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (Action<SnowFluentValidationOptions> action in _actions)
        {
            action(options);
        }
    }
}
