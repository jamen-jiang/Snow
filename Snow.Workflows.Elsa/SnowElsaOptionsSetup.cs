namespace Snow.Workflows.Elsa;

/// <summary>
/// 收集宿主在 PreConfigureServices 阶段给出的选项配置动作。
/// 之所以要这一层: Snow 先跑完所有模块的 PreConfigureServices, 再逐个跑 ConfigureServices,
/// 所以宿主可以在自己模块的 PreConfigureServices 里调用 AddSnowElsa(...), 而这里收集到的动作
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
