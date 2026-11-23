namespace Snow.Workflows.Elsa;

/// <summary>
/// 收集宿主在 <c>AddApplication</c> 之前给出的选项配置动作。
/// 之所以要这一层: AddApplication 里各模块的 ConfigureServices 按依赖顺序逐个执行,
/// 所以宿主要在调用 AddApplication 之前调用 AddSnowElsa(...), 这里收集到的动作
/// 会在 SnowElsaModule.ConfigureServices 里被应用 —— 那时才真正注册 Elsa 的服务。
/// </summary>
public sealed class SnowElsaOptionsSetup
{
    private readonly List<Action<SnowElsaOptions>> _actions = new();

    /// <summary>
    /// 追加一个配置动作。
    /// </summary>
    public void Add(Action<SnowElsaOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
    }

    /// <summary>
    /// 按登记顺序应用全部动作。
    /// </summary>
    public void Apply(SnowElsaOptions options)
    {
        foreach (Action<SnowElsaOptions> action in _actions)
        {
            action(options);
        }
    }
}
