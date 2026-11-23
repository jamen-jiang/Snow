using Microsoft.EntityFrameworkCore;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// DbContext 配置集合(对齐 ABP 的 <c>AbpDbContextOptions</c>):
/// <para>按上下文类型登记配置动作, 未单独登记的类型回落到默认动作。</para>
/// </summary>
public sealed class SnowDbContextOptions
{
    internal List<Action<SnowDbContextConfigurationContext>> DefaultPreConfigureActions { get; } = [];

    internal Action<SnowDbContextConfigurationContext>? DefaultConfigureAction { get; private set; }

    internal Dictionary<Type, List<Action<SnowDbContextConfigurationContext>>> PreConfigureActions { get; } = [];

    internal Dictionary<Type, Action<SnowDbContextConfigurationContext>> ConfigureActions { get; } = [];

    /// <summary>
    /// 追加一个在所有上下文之前执行的预配置动作
    /// </summary>
    /// <param name="action">配置动作</param>
    public void PreConfigure(Action<SnowDbContextConfigurationContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        DefaultPreConfigureActions.Add(action);
    }

    /// <summary>
    /// 设置所有上下文的默认配置动作(通常写提供程序选择)
    /// </summary>
    /// <param name="action">配置动作</param>
    public void Configure(Action<SnowDbContextConfigurationContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        DefaultConfigureAction = action;
    }

    /// <summary>
    /// 追加指定上下文的预配置动作
    /// </summary>
    /// <typeparam name="TDbContext">上下文类型</typeparam>
    /// <param name="action">配置动作</param>
    public void PreConfigure<TDbContext>(Action<SnowDbContextConfigurationContext<TDbContext>> action)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!PreConfigureActions.TryGetValue(typeof(TDbContext), out List<Action<SnowDbContextConfigurationContext>>? actions))
        {
            actions = [];
            PreConfigureActions[typeof(TDbContext)] = actions;
        }

        actions.Add(context => action(new SnowDbContextConfigurationContext<TDbContext>(context)));
    }

    /// <summary>
    /// 设置指定上下文的配置动作
    /// </summary>
    /// <typeparam name="TDbContext">上下文类型</typeparam>
    /// <param name="action">配置动作</param>
    public void Configure<TDbContext>(Action<SnowDbContextConfigurationContext<TDbContext>> action)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(action);

        ConfigureActions[typeof(TDbContext)] = context => action(new SnowDbContextConfigurationContext<TDbContext>(context));
    }

    /// <summary>
    /// 判断指定上下文是否已单独配置
    /// </summary>
    /// <typeparam name="TDbContext">上下文类型</typeparam>
    /// <returns>已配置返回 true</returns>
    public bool IsConfigured<TDbContext>()
    {
        return IsConfigured(typeof(TDbContext));
    }

    /// <summary>
    /// 判断指定上下文是否已单独配置
    /// </summary>
    /// <param name="dbContextType">上下文类型</param>
    /// <returns>已配置返回 true</returns>
    public bool IsConfigured(Type dbContextType)
    {
        ArgumentNullException.ThrowIfNull(dbContextType);

        return ConfigureActions.ContainsKey(dbContextType);
    }

    /// <summary>
    /// 判断是否设置了默认配置动作
    /// </summary>
    /// <returns>已设置返回 true</returns>
    public bool IsConfiguredDefault()
    {
        return DefaultConfigureAction is not null;
    }
}
