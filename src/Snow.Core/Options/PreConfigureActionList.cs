using System;
using System.Collections.Generic;

namespace Snow.Options;

/// <summary>
/// 预配置动作集合(对应 ABP 的 <c>PreConfigureActionList</c>): 收集一批对选项实例的动作, 到时机成熟统一执行。
/// </summary>
/// <typeparam name="TOptions">选项类型</typeparam>
public class PreConfigureActionList<TOptions>
{
    private readonly List<Action<TOptions>> _actions = new();

    /// <summary>
    /// 追加一个预配置动作。
    /// </summary>
    public void Add(Action<TOptions> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _actions.Add(action);
    }

    /// <summary>
    /// 依次把全部动作应用到给定选项实例。
    /// </summary>
    public void Configure(TOptions options)
    {
        foreach (Action<TOptions> action in _actions)
        {
            action(options);
        }
    }

    /// <summary>
    /// 已收集的动作(只读)。
    /// </summary>
    public IReadOnlyList<Action<TOptions>> Actions => _actions;
}
