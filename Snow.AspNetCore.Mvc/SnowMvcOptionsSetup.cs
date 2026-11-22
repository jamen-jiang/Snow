namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 收集宿主在 <c>AddApplication</c> 之前追加的选项配置动作
/// <para>必须在所有模块的 ConfigureServices 之前登记, 否则本模块读到的是空列表。</para>
/// </summary>
public sealed class SnowMvcOptionsSetup
{
    private readonly List<Action<SnowMvcOptions>> _actions = [];

    /// <summary>
    /// 追加一个配置动作
    /// </summary>
    /// <param name="action">配置动作</param>
    public void Add(Action<SnowMvcOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
    }

    /// <summary>
    /// 依次应用全部配置动作
    /// </summary>
    /// <param name="options">目标选项对象</param>
    public void Apply(SnowMvcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (Action<SnowMvcOptions> action in _actions)
        {
            action(options);
        }
    }
}
